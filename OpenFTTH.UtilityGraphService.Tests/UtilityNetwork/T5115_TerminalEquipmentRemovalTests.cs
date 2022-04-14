using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.TestData;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions.Ordering;

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(5115)]
    public class T5115_TerminalEquipmentRemovalTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public T5115_TerminalEquipmentRemovalTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }


        [Fact, Order(1)]
        public async Task RemoveSpliceClosure5FromCC1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CC_1;

            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get splice closure 2
            var sutTerminalEquipment = utilityNetwork.TerminalEquipmentByEquipmentId.Values.First(e => e.Name == "CC1 Splice Closure 5");

            // Remove it
            var removeCmd = new RemoveTerminalEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutTerminalEquipment.Id);

            var removeResult = await _commandDispatcher.HandleAsync<RemoveTerminalEquipment, Result>(removeCmd);

            // Assert
            removeResult.IsSuccess.Should().BeTrue();

            // Check that equipment is removed from utility network projection
            utilityNetwork.TryGetEquipment<TerminalEquipment>(sutTerminalEquipment.Id, out var _).Should().BeFalse();

            // Check that terminals are removed from graph
            foreach (var terminalStructure in sutTerminalEquipment.TerminalStructures)
            {
                foreach (var terminal in terminalStructure.Terminals)
                {
                    utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(terminal.Id, out _).Should().BeFalse();
                }
            }

            // Get node container after terminal equipment removal
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainerAfterTerminalEquipmentRemoval);

            // Check that terminal equipment reference is removed from node container
            nodeContainerAfterTerminalEquipmentRemoval.TerminalEquipmentReferences.Should().NotContain(sutTerminalEquipment.Id);

        }


        [Fact, Order(2)]
        public async Task RemoveTerminalEquipmentAtPos0FromRackInJ1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.J_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_J_1;

            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get equipment on position 1
            var rackMountedEquipmentIdToBeRemoved = nodeContainer.Racks.First().SubrackMounts.First(s => s.Position == 0).TerminalEquipmentId;

            utilityNetwork.TryGetEquipment<TerminalEquipment>(rackMountedEquipmentIdToBeRemoved, out var sutTerminalEquipment);

            // Remove it
            var removeCmd = new RemoveTerminalEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutTerminalEquipment.Id);

            var removeResult = await _commandDispatcher.HandleAsync<RemoveTerminalEquipment, Result>(removeCmd);

            // Assert
            removeResult.IsSuccess.Should().BeTrue();

            // Check that equipment is removed from utility network projection
            utilityNetwork.TryGetEquipment<TerminalEquipment>(sutTerminalEquipment.Id, out var _).Should().BeFalse();

            // Check that terminals are removed from graph
            foreach (var terminalStructure in sutTerminalEquipment.TerminalStructures)
            {
                foreach (var terminal in terminalStructure.Terminals)
                {
                    utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(terminal.Id, out _).Should().BeFalse();
                }
            }

            // Get node container after terminal equipment removal
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainerAfterTerminalEquipmentRemoval);

            // Check that subrack mount is removed from node container
            nodeContainerAfterTerminalEquipmentRemoval.Racks.First().SubrackMounts.Any(s => s.TerminalEquipmentId == rackMountedEquipmentIdToBeRemoved);

        }



        [Fact, Order(10)]
        public async Task RemoveFirstClosureFromCC1_ShouldFail_BecauseItHasConnections()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeId = TestRouteNetwork.CC_1;
            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CC_1;

            // Get node container
            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainer);

            // Get splice closure 1
            var sutTerminalEquipment = utilityNetwork.TerminalEquipmentByEquipmentId.Values.First(e => e.Name == "CC1 Splice Closure 1");

            // Remove it
            var removeCmd = new RemoveTerminalEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutTerminalEquipment.Id);

            var removeResult = await _commandDispatcher.HandleAsync<RemoveTerminalEquipment, Result>(removeCmd);

            // Assert
            removeResult.IsFailed.Should().BeTrue();

        }



    }
}

#nullable enable
