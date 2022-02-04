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
    [Order(8200)]
    public class T8200_ConnectivityTraceTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public T8200_ConnectivityTraceTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

  

        [Fact, Order(1)]
        public async void TerminalEquipmentConnectivityTraceInCC1_ShouldSucceed()
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

            var terminalEquipmentFace = connectivityFaces.First(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment);

            utilityNetwork.TryGetEquipment<TerminalEquipment>(terminalEquipmentFace.EquipmentId, out var terminalEquipment);

            // Get connectivity trace
            var connectivityTraceQuery = new GetConnectivityTraceView(sutRouteNodeId, terminalEquipment.TerminalStructures[0].Terminals[0].Id);

            var connectivityTraceQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityTraceView, Result<ConnectivityTraceView>> (
                connectivityTraceQuery
            );


        }




    }
}
