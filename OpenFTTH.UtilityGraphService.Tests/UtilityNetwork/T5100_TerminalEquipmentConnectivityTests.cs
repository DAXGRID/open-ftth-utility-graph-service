﻿using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.TestData;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(5100)]
    public class T5100_TerminalEquipmentConnectivityTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public T5100_TerminalEquipmentConnectivityTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void ConnectFirstTerminalEquipmentInCC1WithFiberCable_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CC_1;
            var sutCableName = "K69373563";


            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get equipment
            utilityNetwork.TryGetEquipment<TerminalEquipment>(nodeContainer.TerminalEquipmentReferences.First(), out var terminalEquipment);

            // Get cable
            var connectivityQuery = new GetConnectivityFaces(nodeContainer.RouteNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var cableId = viewModel.First(m => m.EquipmentName.StartsWith(sutCableName)).EquipmentId;

            utilityNetwork.TryGetEquipment<SpanEquipment>(cableId, out var spanEquipment);


            // ACT (do the connect between cable and equipment)
            var connectCmd = new ConnectSpanEquipmentAndTerminalEquipment(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                spanEquipmentId: spanEquipment.Id,
                spanSegmentsIds: new Guid[] { 
                    spanEquipment.SpanStructures[2].SpanSegments[0].Id, // Fiber 2
                    spanEquipment.SpanStructures[3].SpanSegments[0].Id  // Fiber 3
                },
                terminalEquipmentId: terminalEquipment.Id,
                terminalIds: new Guid[] { 
                    terminalEquipment.TerminalStructures[0].Terminals[0].Id,  // Pin 1
                    terminalEquipment.TerminalStructures[0].Terminals[5].Id   // Pin 4
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanEquipmentAndTerminalEquipment, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();

            // Trace fiber 1 (should not be connected to anything)
            var fiber1TraceResult = utilityNetwork.Graph.Trace(spanEquipment.SpanStructures[1].SpanSegments[0].Id);

            fiber1TraceResult.Upstream.Length.Should().Be(0);
            fiber1TraceResult.Downstream.Length.Should().Be(0);

            // Trace fiber 2
            var fiber2TraceResult = utilityNetwork.Graph.Trace(spanEquipment.SpanStructures[2].SpanSegments[0].Id);

            var upstreamTerminalFromTrace = fiber2TraceResult.Upstream.First(t => t.Id == terminalEquipment.TerminalStructures[0].Terminals[0].Id) as IUtilityGraphTerminalRef;

            var equipmentFromTracedTerminal = upstreamTerminalFromTrace.TerminalEquipment(utilityNetwork);

            equipmentFromTracedTerminal.Should().Be(terminalEquipment);

            // Trace terminal 4
            var term4TraceResult = utilityNetwork.Graph.Trace(terminalEquipment.TerminalStructures[0].Terminals[5].Id);

            term4TraceResult.Downstream.Length.Should().Be(0);
            term4TraceResult.Upstream.Length.Should().Be(2); // a segment and a terminal at the end
            ((UtilityGraphConnectedTerminal)term4TraceResult.Upstream.Last()).RouteNodeId.Should().NotBeEmpty();


            // Check equipment connectivity view
            var connectivityViewQuery = new GetTerminalEquipmentConnectivityView(sutNodeId, terminalEquipment.Id);

            var connectivityViewResult = await _queryDispatcher.HandleAsync<GetTerminalEquipmentConnectivityView, Result<TerminalEquipmentAZConnectivityViewModel>>(
                connectivityViewQuery
            );

            connectivityViewResult.IsSuccess.Should().BeTrue();

            var connectivityTraceView = connectivityViewResult.Value;

            var teInfoToAssert = connectivityTraceView.TerminalEquipments.First(t => t.Id == terminalEquipment.Id);

            teInfoToAssert.TerminalStructures[0].Lines[0].Z.Should().NotBeNull();
            teInfoToAssert.TerminalStructures[0].Lines[0].Z.ConnectedTo.Should().NotBeNull();
            teInfoToAssert.TerminalStructures[0].Lines[0].Z.ConnectedTo.Should().Be($"{sutCableName} (72) Fiber 2");

            // Check faces and face connections
            var connectivityFaceQuery = new GetConnectivityFaces(sutNodeId);

            var connectivityFaceQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            var spanEquipmentFace = connectivityFaceQueryResult.Value.First(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.SpanEquipment);

            // Get face connections for span equipment in CC_1 (where it is spliced)
            var spanEquipmentConnectionsQueryInCC1 = new GetConnectivityFaceConnections(sutNodeId, spanEquipmentFace.EquipmentId, spanEquipmentFace.FaceKind);

            var spanEquipmentConnectionsQueryInCC1Result = await _queryDispatcher.HandleAsync<GetConnectivityFaceConnections, Result<List<ConnectivityFaceConnection>>>(
                spanEquipmentConnectionsQueryInCC1
            );

            spanEquipmentConnectionsQueryInCC1Result.IsSuccess.Should().BeTrue();

            var spanEquipmentConnectionsInCC1 = spanEquipmentConnectionsQueryInCC1Result.Value;

            spanEquipmentConnectionsInCC1[0].IsConnected.Should().BeFalse();
            spanEquipmentConnectionsInCC1[1].IsConnected.Should().BeTrue();
            spanEquipmentConnectionsInCC1[2].IsConnected.Should().BeTrue();
            spanEquipmentConnectionsInCC1[3].IsConnected.Should().BeFalse();

            // Get face connections for span equipment in CO_1 (where it is not spliced)
            var spanEquipmentConnectionsQueryInCO1 = new GetConnectivityFaceConnections(TestRouteNetwork.CO_1, spanEquipmentFace.EquipmentId, spanEquipmentFace.FaceKind);

            var spanEquipmentConnectionsQueryInCO1Result = await _queryDispatcher.HandleAsync<GetConnectivityFaceConnections, Result<List<ConnectivityFaceConnection>>>(
                spanEquipmentConnectionsQueryInCO1
            );

            spanEquipmentConnectionsQueryInCO1Result.IsSuccess.Should().BeTrue();

            var spanEquipmentConnectionsInCO1 = spanEquipmentConnectionsQueryInCO1Result.Value;

            spanEquipmentConnectionsInCO1[0].IsConnected.Should().BeFalse();
            spanEquipmentConnectionsInCO1[1].IsConnected.Should().BeFalse();
            spanEquipmentConnectionsInCO1[2].IsConnected.Should().BeFalse();
            spanEquipmentConnectionsInCO1[3].IsConnected.Should().BeFalse();


        }

        [Fact, Order(2)]
        public async void ConnectFirstTerminalEquipmentInCO1WithFiberCable_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CO_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CO_1;
            var sutCableName = "K69373563";


            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get equipment
            utilityNetwork.TryGetEquipment<TerminalEquipment>(nodeContainer.TerminalEquipmentReferences.First(), out var terminalEquipment);

            // Get cable
            var connectivityQuery = new GetConnectivityFaces(nodeContainer.RouteNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var cableId = viewModel.First(m => m.EquipmentName.StartsWith(sutCableName)).EquipmentId;

            utilityNetwork.TryGetEquipment<SpanEquipment>(cableId, out var cableSpanEquipment);


            // ACT (do the connect between cable and equipment)
            var connectCmd = new ConnectSpanEquipmentAndTerminalEquipment(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                spanEquipmentId: cableSpanEquipment.Id,
                spanSegmentsIds: new Guid[] {
                    cableSpanEquipment.SpanStructures[2].SpanSegments[0].Id, // Fiber 2
                    cableSpanEquipment.SpanStructures[3].SpanSegments[0].Id  // Fiber 3
                },
                terminalEquipmentId: terminalEquipment.Id,
                terminalIds: new Guid[] {
                    terminalEquipment.TerminalStructures[1].Terminals[0].Id,  // Tray 1 Pin 1
                    terminalEquipment.TerminalStructures[1].Terminals[1].Id   // Tray 1 Pin 2
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanEquipmentAndTerminalEquipment, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();

            // Trace tray 1 fiber 1 (should not be connected to anything)
            var fiber1TraceResult = utilityNetwork.Graph.Trace(cableSpanEquipment.SpanStructures[1].SpanSegments[0].Id);

            fiber1TraceResult.Upstream.Length.Should().Be(0);
            fiber1TraceResult.Downstream.Length.Should().Be(0);

            // Trace 2
            var fiber2TraceResult = utilityNetwork.Graph.Trace(cableSpanEquipment.SpanStructures[2].SpanSegments[0].Id);

            var downstreamTerminalFromTrace = fiber2TraceResult.Downstream.First(t => t.Id == terminalEquipment.TerminalStructures[1].Terminals[0].Id) as IUtilityGraphTerminalRef;

            var equipmentFromTracedTerminal = downstreamTerminalFromTrace.TerminalEquipment(utilityNetwork);

            equipmentFromTracedTerminal.Should().Be(terminalEquipment);

            // Trace tray 1 terminal 1
            var term4TraceResult = utilityNetwork.Graph.Trace(terminalEquipment.TerminalStructures[1].Terminals[0].Id);

            term4TraceResult.Downstream.Length.Should().Be(0);
            term4TraceResult.Upstream.Length.Should().Be(2); // a segment and a terminal at the end
            ((UtilityGraphConnectedTerminal)term4TraceResult.Upstream.Last()).RouteNodeId.Should().NotBeEmpty();



            // Check faces and face connections
            var connectivityFaceQuery = new GetConnectivityFaces(sutNodeId);

            var connectivityFaceQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            var spanEquipmentFace = connectivityFaceQueryResult.Value.First(f => f.EquipmentId == cableSpanEquipment.Id);

            // Get face connections for span equipment in CO_1 (where it is spliced)
            var spanEquipmentConnectionsQueryInCO1 = new GetConnectivityFaceConnections(sutNodeId, spanEquipmentFace.EquipmentId, spanEquipmentFace.FaceKind);

            var spanEquipmentConnectionsQueryInCO1Result = await _queryDispatcher.HandleAsync<GetConnectivityFaceConnections, Result<List<ConnectivityFaceConnection>>>(
                spanEquipmentConnectionsQueryInCO1
            );

            spanEquipmentConnectionsQueryInCO1Result.IsSuccess.Should().BeTrue();

            var spanEquipmentConnectionsInCO1 = spanEquipmentConnectionsQueryInCO1Result.Value;

            spanEquipmentConnectionsInCO1[0].IsConnected.Should().BeFalse();
            spanEquipmentConnectionsInCO1[1].IsConnected.Should().BeTrue();
            spanEquipmentConnectionsInCO1[2].IsConnected.Should().BeTrue();
            spanEquipmentConnectionsInCO1[3].IsConnected.Should().BeFalse();
        }

        [Fact, Order(3)]
        public async void ConnectFirstRackEquipmentInCO1WithFiberCable_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CO_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CO_1;
            var sutCableName = "K69373563";


            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get equipment
            utilityNetwork.TryGetEquipment<TerminalEquipment>(nodeContainer.Racks[0].SubrackMounts.First().TerminalEquipmentId, out var terminalEquipment);

            // Get cable
            var connectivityQuery = new GetConnectivityFaces(nodeContainer.RouteNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var cableId = viewModel.First(m => m.EquipmentName.StartsWith(sutCableName)).EquipmentId;

            utilityNetwork.TryGetEquipment<SpanEquipment>(cableId, out var cableSpanEquipment);


            // ACT (do the connect between cable and equipment)
            var connectCmd = new ConnectSpanEquipmentAndTerminalEquipment(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                spanEquipmentId: cableSpanEquipment.Id,
                spanSegmentsIds: new Guid[] {
                    cableSpanEquipment.SpanStructures[4].SpanSegments[0].Id, // Fiber 4
                },
                terminalEquipmentId: terminalEquipment.Id,
                terminalIds: new Guid[] {
                    terminalEquipment.TerminalStructures[0].Terminals[0].Id,  // Tray 1 Pin 1
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanEquipmentAndTerminalEquipment, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();

         }


        [Fact, Order(50)]
        public async void TestTrace_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CO_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CO_1;
            var sutCableName = "K69373563";
        
            var cable = FindSpanEquipmentRelatedToRouteNetworkElementByName(sutNodeId, sutCableName);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new EquipmentIdList() { cable.Id })
                {
                    EquipmentDetailsFilter = new EquipmentDetailsFilterOptions()
                    {
                        IncludeRouteNetworkTrace = true
                    }
                }
            );

            // Assert
            equipmentQueryResult.IsSuccess.Should().BeTrue();

            //equipmentQueryResult.Value.RouteNetworkTraces.Should().NotBeNull();

        }


        [Fact, Order(100)]
        public async void CheckThatLISAInJ1Has24PatchesAnd24SplicesInTray_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.J_1;

            var connectivityQuery = new GetConnectivityFaces(sutNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var face = viewModel.First(f => f.EquipmentName.StartsWith("LISA APC"));


            // Check equipment connectivity view
            var connectivityViewQuery = new GetTerminalEquipmentConnectivityView(sutNodeId, face.EquipmentId);

            var connectivityViewResult = await _queryDispatcher.HandleAsync<GetTerminalEquipmentConnectivityView, Result<TerminalEquipmentAZConnectivityViewModel>>(
                connectivityViewQuery
            );

            connectivityViewResult.IsSuccess.Should().BeTrue();

            var connectivityTraceView = connectivityViewResult.Value;

            connectivityTraceView.TerminalEquipments.First().TerminalStructures.First().Lines.Count(l => (l.A != null && l.A.FaceKind == FaceKindEnum.SpliceSide) || (l.Z != null && l.Z.FaceKind == FaceKindEnum.SpliceSide)).Should().Be(24);
            connectivityTraceView.TerminalEquipments.First().TerminalStructures.First().Lines.Count(l => (l.A != null && l.A.FaceKind == FaceKindEnum.PatchSide) || (l.Z != null && l.Z.FaceKind == FaceKindEnum.PatchSide)).Should().Be(24);
        }



        [Fact, Order(101)]
        public async void CheckThatBUDIInCC1Has0Patches12SplicesInTray_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;

            var connectivityQuery = new GetConnectivityFaces(sutNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var face = viewModel.First(f => f.EquipmentName.StartsWith("BUDI"));


            // Check equipment connectivity view
            var connectivityViewQuery = new GetTerminalEquipmentConnectivityView(sutNodeId, face.EquipmentId);

            var connectivityViewResult = await _queryDispatcher.HandleAsync<GetTerminalEquipmentConnectivityView, Result<TerminalEquipmentAZConnectivityViewModel>>(
                connectivityViewQuery
            );

            connectivityViewResult.IsSuccess.Should().BeTrue();

            var connectivityTraceView = connectivityViewResult.Value;

            connectivityTraceView.TerminalEquipments.First().TerminalStructures.First().Lines.Count(l => (l.A != null && l.A.FaceKind == FaceKindEnum.SpliceSide) || (l.Z != null && l.Z.FaceKind == FaceKindEnum.SpliceSide)).Should().Be(12);
            connectivityTraceView.TerminalEquipments.First().TerminalStructures.First().Lines.Count(l => (l.A != null && l.A.FaceKind == FaceKindEnum.PatchSide) || (l.Z != null && l.Z.FaceKind == FaceKindEnum.PatchSide)).Should().Be(0);
        }


        [Fact, Order(102)]
        public async void GetConnectivityTraceView_ShouldSucceed()
        {
            var connectivityTrace = new GetConnectivityTraceView(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityTraceView, Result<ConnectivityTraceView>>(
                connectivityTrace
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;
        }



        private SpanEquipment? FindSpanEquipmentRelatedToRouteNetworkElementByName(Guid routeNetworkElementId, string spanEquipmentName)
        {
            var routeNetworkQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
              new GetRouteNetworkDetails(new RouteNetworkElementIdList() { routeNetworkElementId })
              {
                  RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementAndInterestObjects
              }
            ).Result;

            InterestIdList interestIdList = new InterestIdList();
            foreach (var interestRel in routeNetworkQueryResult.Value.RouteNetworkElements[routeNetworkElementId].InterestRelations)
            {
                interestIdList.Add(interestRel.RefId);
            }

            var equipmentQueryResult = _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                    new GetEquipmentDetails(interestIdList)
                    {
                        EquipmentDetailsFilter = new EquipmentDetailsFilterOptions() { IncludeRouteNetworkTrace = true }
                    }
                ).Result;

            foreach (var spanEquipment in equipmentQueryResult.Value.SpanEquipment)
            {
                if (spanEquipment.Name == spanEquipmentName)
                    return spanEquipment;
            }

            return null;
        }



    }
}
