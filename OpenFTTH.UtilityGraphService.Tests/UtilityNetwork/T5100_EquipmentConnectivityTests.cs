using FluentAssertions;
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
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions.Ordering;

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(5100)]
    public class T5100_EquipmentConnectivityTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public T5100_EquipmentConnectivityTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async Task ConnectFirstTerminalEquipmentInCC1WithFiberCable_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CC_1;
            var sutCableName = "K69373563";


            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get equipment
            var terminalEquipment = utilityNetwork.TerminalEquipmentByEquipmentId.Values.First(e => e.Name == "CC1 Splice Closure 1");

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
            var connectCmd = new ConnectSpanSegmentsWithTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                connects: new ConnectSpanSegmentToTerminalOperation[]
                {
                    // Fiber 2 -> Pin 1
                    new ConnectSpanSegmentToTerminalOperation(spanEquipment.SpanStructures[2].SpanSegments[0].Id, terminalEquipment.TerminalStructures[0].Terminals[0].Id),

                    // Fiber 3 -> Pin 6
                    new ConnectSpanSegmentToTerminalOperation(spanEquipment.SpanStructures[3].SpanSegments[0].Id, terminalEquipment.TerminalStructures[0].Terminals[5].Id),
   
                    // Fiber 12 -> Pin 12
                    new ConnectSpanSegmentToTerminalOperation(spanEquipment.SpanStructures[12].SpanSegments[0].Id, terminalEquipment.TerminalStructures[0].Terminals[11].Id)

                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsWithTerminalsAtRouteNode, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(terminalEquipment.TerminalStructures[0].Terminals[0].Id, out var terminalRef);


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

            teInfoToAssert.TerminalStructures[0].Lines[0].A.Should().NotBeNull();
            teInfoToAssert.TerminalStructures[0].Lines[0].A.ConnectedTo.Should().NotBeNull();
            teInfoToAssert.TerminalStructures[0].Lines[0].A.ConnectedTo.Should().Be($"{sutCableName} (72) Tube 1 Fiber 2");

            // Check faces and face connections
            var connectivityFaceQuery = new GetConnectivityFaces(sutNodeId);

            var connectivityFaceQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            var spanEquipmentFace = connectivityFaceQueryResult.Value.First(f => f.EquipmentName.StartsWith("K69373563"));

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
        public async Task ConnectFirstTerminalEquipmentInCO1WithFiberCable_ShouldSucceed()
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
            var connectCmd = new ConnectSpanSegmentsWithTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                connects: new ConnectSpanSegmentToTerminalOperation[]
                {
                    // Fiber 2 -> Tray 2 Pin 1
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[2].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[0].Id),

                    // Fiber 3 -> Tray 2 Pin 2
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[3].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[1].Id),

                    // Fiber 4 -> Tray 2 Pin 3
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[4].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[2].Id),

                    // Fiber 5 -> Tray 2 Pin 4
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[5].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[3].Id)
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsWithTerminalsAtRouteNode, Result>(connectCmd);

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
            spanEquipmentConnectionsInCO1[3].IsConnected.Should().BeTrue();
            spanEquipmentConnectionsInCO1[4].IsConnected.Should().BeTrue();
            spanEquipmentConnectionsInCO1[5].IsConnected.Should().BeFalse();
        }


        [Fact, Order(3)]
        public async Task ConnectFirstRackEquipmentInCO1WithFiberCable_ShouldSucceed()
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
            var connectCmd = new ConnectSpanSegmentsWithTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                connects: new ConnectSpanSegmentToTerminalOperation[]
                {
                    // Fiber 12 -> Tray 1 Pin 1
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[12].SpanSegments[0].Id, terminalEquipment.TerminalStructures[0].Terminals[0].Id)
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsWithTerminalsAtRouteNode, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();

        }

        [Fact, Order(4)]
        public async Task ConnectFirstTerminalEquipmentInCC1WithCustomerFiberCable_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CC_1;
            var sutCableName = "K12345678";


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
            var connectCmd = new ConnectSpanSegmentsWithTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                connects: new ConnectSpanSegmentToTerminalOperation[]
                {
                    // Fiber 1 -> Tray 1 Pin 12
                    new ConnectSpanSegmentToTerminalOperation(spanEquipment.SpanStructures[1].SpanSegments[0].Id, terminalEquipment.TerminalStructures[0].Terminals[11].Id),
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsWithTerminalsAtRouteNode, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();
        }

        [Fact, Order(5)]
        public async Task ConnectFirstTerminalEquipmentInSDU1WithCustomerFiberCable_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.SDU_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_SDU_1;
            var sutCableName = "K12345678";


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
            var connectCmd = new ConnectSpanSegmentsWithTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                connects: new ConnectSpanSegmentToTerminalOperation[]
                {
                    // Fiber 1 -> Tray 1 Pin 1
                    new ConnectSpanSegmentToTerminalOperation(spanEquipment.SpanStructures[1].SpanSegments[0].Id, terminalEquipment.TerminalStructures[0].Terminals[0].Id),
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsWithTerminalsAtRouteNode, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();
        }


        [Fact, Order(50)]
        public async Task DisconnectFirstTerminalEquipmentInCO1WithFiberCable_ShouldSucceed()
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

            utilityNetwork.TryGetEquipment<SpanEquipment>(cableId, out var cableBeforeDisconnect);

            var beforeTrace = utilityNetwork.Graph.Trace(cableBeforeDisconnect.SpanStructures[2].SpanSegments[0].Id);
            beforeTrace.All.Any(g => g.Id == terminalEquipment.TerminalStructures[1].Terminals[0].Id).Should().BeTrue();


            // ACT (do the connect between cable and equipment)
            var disconnectCmd = new DisconnectSpanSegmentsFromTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                disconnects: new DisconnectSpanSegmentFromTerminalOperation[]
                {
                    // Fiber 1 -> Tray 2 Pin 1
                    new DisconnectSpanSegmentFromTerminalOperation(cableBeforeDisconnect.SpanStructures[2].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[0].Id),
                    // Fiber 2 -> Tray 2 Pin 2
                    new DisconnectSpanSegmentFromTerminalOperation(cableBeforeDisconnect.SpanStructures[3].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[1].Id)
                }
            );
            var disconnectCmdResult = await _commandDispatcher.HandleAsync<DisconnectSpanSegmentsFromTerminalsAtRouteNode, Result>(disconnectCmd);

            // Assert
            disconnectCmdResult.IsSuccess.Should().BeTrue();

            utilityNetwork.TryGetEquipment<SpanEquipment>(cableId, out var cableAfterDisconnect);

            cableAfterDisconnect.SpanStructures[2].SpanSegments[0].FromTerminalId.Should().BeEmpty();
            cableAfterDisconnect.SpanStructures[3].SpanSegments[0].FromTerminalId.Should().BeEmpty();
        
            var afterTraceFiber2 = utilityNetwork.Graph.Trace(cableAfterDisconnect.SpanStructures[2].SpanSegments[0].Id);
            afterTraceFiber2.All.Any(g => g.Id == terminalEquipment.TerminalStructures[1].Terminals[0].Id).Should().BeFalse();

            var afterTraceFiber3 = utilityNetwork.Graph.Trace(cableAfterDisconnect.SpanStructures[3].SpanSegments[0].Id);
            afterTraceFiber2.All.Any(g => g.Id == terminalEquipment.TerminalStructures[1].Terminals[1].Id).Should().BeFalse();
        }


        [Fact, Order(51)]
        public async Task ConnectFirstTerminalEquipmentInCO1WithFiberCableAgain_ShouldSucceed()
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
            var connectCmd = new ConnectSpanSegmentsWithTerminalsAtRouteNode(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                connects: new ConnectSpanSegmentToTerminalOperation[]
                {
                    // Fiber 2 -> Tray 2 Pin 1
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[2].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[0].Id),

                    // Fiber 3 -> Tray 2 Pin 2
                    new ConnectSpanSegmentToTerminalOperation(cableSpanEquipment.SpanStructures[3].SpanSegments[0].Id, terminalEquipment.TerminalStructures[1].Terminals[1].Id)
                }
            );
            var connectCmdResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsWithTerminalsAtRouteNode, Result>(connectCmd);

            // Assert
            connectCmdResult.IsSuccess.Should().BeTrue();

            var trace = utilityNetwork.Graph.Trace(cableSpanEquipment.SpanStructures[2].SpanSegments[0].Id);
            trace.All.Any(g => g.Id == terminalEquipment.TerminalStructures[1].Terminals[0].Id).Should().BeTrue();

        }



        [Fact, Order(1000)]
        public async Task TestTrace_ShouldSucceed()
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



        [Fact, Order(1001)]
        public async Task CheckThatLISAInJ1Has24PatchesAnd24SplicesInTray_ShouldSucceed()
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



        [Fact, Order(1002)]
        public async Task CheckThatBUDIInCC1Has0Patches12SplicesInTray_ShouldSucceed()
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
