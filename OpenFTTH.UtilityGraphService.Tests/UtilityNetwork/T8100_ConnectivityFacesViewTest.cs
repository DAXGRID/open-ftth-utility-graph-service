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
            var sutRouteNodeId = TestRouteNetwork.J_1;

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            // Get faces
            var connectivityFaceQuery = new GetConnectivityFaces(sutRouteNodeId);

            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var connectivityFaces = connectivityQueryResult.Value;

            connectivityFaces.Count.Should().BeGreaterThan(4);

            // Get face connections
            var connectivityFaceConnectionsQuery = new GetConnectivityFaceConnections(sutRouteNodeId, connectivityFaces[0].EquipmentId, connectivityFaces[0].DirectionType);

            var connectivityFaceConnectionsQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaceConnections, Result<List<ConnectivityFaceConnection>>>(
                connectivityFaceConnectionsQuery
            );

            connectivityFaceConnectionsQueryResult.IsSuccess.Should().BeTrue();

            var faceConnections = connectivityFaceConnectionsQueryResult.Value;

            faceConnections.Count.Should().BeGreaterThan(4);

        }



        [Fact, Order(2)]
        public async void QueryConnectivityFacesInCC1_ShouldSucceed()
        {
            // Setup
            var sutRouteNodeId = TestRouteNetwork.CC_1;

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();


            var connectivityQuery = new GetConnectivityFaces(sutRouteNodeId);


            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityQuery
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

        }
    }
}
