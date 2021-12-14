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
            var sutNodeContainer = TestUtilityNetwork.NodeContainer_J_1;

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainer, out var nodeContainerBeforeCommand);

            var firstEquipmentInRack1Id = nodeContainerBeforeCommand.Racks[0].Id;
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
