using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
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
        public async void ConnectFirstTerminalEquipmentInCC1With_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CC_1;

            
            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get equipment
            utilityNetwork.TryGetEquipment<TerminalEquipment>(nodeContainer.TerminalEquipmentReferences.First(), out var terminalEquipment);

            // Get cable
            var connectivityQuery = new GetConnectivityFaces(nodeContainer.RouteNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<EquipmentConnectivityFace>>>(
                connectivityQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var cableId = viewModel.First(m => m.EquipmentName.StartsWith("K69373563")).EquipmentId;

            utilityNetwork.TryGetEquipment<SpanEquipment>(cableId, out var spanEquipment);


            // ACT (do the connect between cable and equipment)
            var connectCmd = new ConnectSpanEquipmentAndTerminalEquipment(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                routeNodeId: sutNodeId,
                spanEquipmentId: spanEquipment.Id,
                spanSegmentsIds: new Guid[] { 
                    spanEquipment.SpanStructures[1].SpanSegments[0].Id, // Fiber 1
                    spanEquipment.SpanStructures[2].SpanSegments[0].Id  // Fiber 2
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
            
            // trace fiber 1
            var spanTraceResult = utilityNetwork.Graph.Trace(spanEquipment.SpanStructures[1].SpanSegments[0].Id);

            var upstreamTerminalFromTrace = spanTraceResult.Upstream.First(t => t.Id == terminalEquipment.TerminalStructures[0].Terminals[0].Id) as IUtilityGraphTerminalRef;

            var equipmentFromTracedTerminal = upstreamTerminalFromTrace.TerminalEquipment(utilityNetwork);

            equipmentFromTracedTerminal.Should().Be(terminalEquipment);

            // trace terminal 4
            var terj8haiTraceResult = utilityNetwork.Graph.Trace(terminalEquipment.TerminalStructures[0].Terminals[5].Id);


        }


        [Fact, Order(10)]
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


    }
}
