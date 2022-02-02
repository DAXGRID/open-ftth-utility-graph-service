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
    [Order(8010)]
    public class T8100_ConnectivityFacesViewTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public T8100_ConnectivityFacesViewTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

  

        [Fact, Order(1)]
        public async void QueryConnectivityFacesInJ1_ShouldSucceed()
        {
            // Setup
            var sutRouteNodeId = TestRouteNetwork.CC_1;

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            // Get faces
            var connectivityFaceQuery = new GetConnectivityFaces(sutRouteNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var connectivityFaces = connectivityQueryResult.Value;

            connectivityFaces.Count(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment).Should().BeGreaterThan(0);
            connectivityFaces.Count(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.SpanEquipment).Should().BeGreaterThan(0);

            var terminalEquipmentFace = connectivityFaces.First(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment);

            var spanEquipmentFace = connectivityFaces.First(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.SpanEquipment);

            // Get face connections for terminal equipment
            var terminalEquipmentConnectionsQuery = new GetConnectivityFaceConnections(sutRouteNodeId, terminalEquipmentFace.EquipmentId, terminalEquipmentFace.DirectionType);

            var terminalEquipmentConnectionsQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaceConnections, Result<List<ConnectivityFaceConnection>>>(
                terminalEquipmentConnectionsQuery
            );
            terminalEquipmentConnectionsQueryResult.IsSuccess.Should().BeTrue();

            var terminalEquipmentConnections = terminalEquipmentConnectionsQueryResult.Value;

            terminalEquipmentConnections.Count.Should().BeGreaterThan(4);


            // Get face connections for span equipment
            var spanEquipmentConnectionsQuery = new GetConnectivityFaceConnections(sutRouteNodeId, spanEquipmentFace.EquipmentId, spanEquipmentFace.DirectionType);

            var spanEquipmentConnectionsQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaceConnections, Result<List<ConnectivityFaceConnection>>>(
                spanEquipmentConnectionsQuery
            );

            spanEquipmentConnectionsQueryResult.IsSuccess.Should().BeTrue();

            var spanEquipmentConnections = spanEquipmentConnectionsQueryResult.Value;

            spanEquipmentConnections.Count.Should().BeGreaterThan(4);
        }




    }
}
