﻿using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.TestData;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions.Ordering;

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(551)]
    public class T0551_NodeContainerRackTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public T0551_NodeContainerRackTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async Task PlaceFirstRackInContainerInJ2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_J_1;

            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainerBeforeCommand);

            var placeRackCmd = new PlaceRackInNodeContainer(
                Guid.NewGuid(),
                new UserContext("test", Guid.Empty),
                sutNodeContainerId,
                Guid.NewGuid(),
                TestSpecifications.Rack_ESTI,
                "Rack 1",
                80
            );

            var placeRackResult = await _commandDispatcher.HandleAsync<PlaceRackInNodeContainer, Result>(placeRackCmd);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new InterestIdList() { nodeContainerBeforeCommand.InterestId })
            );

            // Assert
            placeRackResult.IsSuccess.Should().BeTrue();
            equipmentQueryResult.IsSuccess.Should().BeTrue();
            var nodeContainerAfterCommand = equipmentQueryResult.Value.NodeContainers[sutNodeContainerId];

            // Check that rack was added to node container
            nodeContainerAfterCommand.Should().NotBeNull();
            nodeContainerAfterCommand.Racks.Should().NotBeNull();
            nodeContainerAfterCommand.Racks[0].Name.Should().Be(placeRackCmd.RackName);
            nodeContainerAfterCommand.Racks[0].Position.Should().Be(1);
            nodeContainerAfterCommand.Racks[0].SpecificationId.Should().Be(placeRackCmd.RackSpecificationId);

            // Check if an event is published to the notification.utility-network topic having an idlist containing the node container we just changed
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.Category == "EquipmentModification" && n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutNodeContainerId)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(nodeContainerBeforeCommand.RouteNodeId);
        }

        [Fact, Order(2)]
        public async Task PlaceSecondRackInContainerInJ2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_J_1;

            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainerBeforeCommand);

            var placeRackCmd = new PlaceRackInNodeContainer(
                Guid.NewGuid(),
                new UserContext("test", Guid.Empty),
                sutNodeContainerId,
                Guid.NewGuid(),
                TestSpecifications.Rack_ESTI,
                "Rack 2",
                80
            );

            var placeRackResult = await _commandDispatcher.HandleAsync<PlaceRackInNodeContainer, Result>(placeRackCmd);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new InterestIdList() { nodeContainerBeforeCommand.InterestId })
            );

            // Assert
            placeRackResult.IsSuccess.Should().BeTrue();
            equipmentQueryResult.IsSuccess.Should().BeTrue();
            var nodeContainerAfterCommand = equipmentQueryResult.Value.NodeContainers[sutNodeContainerId];

            // Check that rack was added to node container
            nodeContainerAfterCommand.Should().NotBeNull();
            nodeContainerAfterCommand.Racks.Should().NotBeNull();
            nodeContainerAfterCommand.Racks.Count().Should().Be(2);

            nodeContainerAfterCommand.Racks[1].Name.Should().Be(placeRackCmd.RackName);
            nodeContainerAfterCommand.Racks[1].Position.Should().Be(2);
            nodeContainerAfterCommand.Racks[1].SpecificationId.Should().Be(placeRackCmd.RackSpecificationId);

            // Check if an event is published to the notification.utility-network topic having an idlist containing the node container we just changed
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.Category == "EquipmentModification" && n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutNodeContainerId)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(nodeContainerBeforeCommand.RouteNodeId);
        }


        [Fact, Order(3)]
        public async Task PlaceRackInCO_1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CO_1;

            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainerBeforeCommand);

            var placeRackCmd = new PlaceRackInNodeContainer(
                Guid.NewGuid(),
                new UserContext("test", Guid.Empty),
                sutNodeContainerId,
                Guid.NewGuid(),
                TestSpecifications.Rack_ESTI,
                "Rack 2",
                80
            );

            var placeRackResult = await _commandDispatcher.HandleAsync<PlaceRackInNodeContainer, Result>(placeRackCmd);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new InterestIdList() { nodeContainerBeforeCommand.InterestId })
            );

            // Assert
            placeRackResult.IsSuccess.Should().BeTrue();
        }



        
        [Fact, Order(4)]
        public async Task PlaceRackEquipmentInCO_1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutNodeContainerId = TestUtilityNetwork.NodeContainer_CO_1;

            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainerId, out var nodeContainerBeforeCommand);

            var placeEquipmentCmd = new PlaceTerminalEquipmentInNodeContainer(
            correlationId: Guid.NewGuid(),
            userContext: new UserContext("test", Guid.Empty),
            nodeContainerId: sutNodeContainerId,
            Guid.NewGuid(),
            terminalEquipmentSpecificationId: TestSpecifications.Subrack_LISA_APC_UPC,
            numberOfEquipments: 80,
            startSequenceNumber: 1,
            namingMethod: TerminalEquipmentNamingMethodEnum.NumberOnly,
            namingInfo: null
             )
            {
                SubrackPlacementInfo = new SubrackPlacementInfo(nodeContainerBeforeCommand.Racks[0].Id, 0, SubrackPlacmentMethod.BottomUp)
            };


            // Act
            var placeEquipmentCmdResult = await _commandDispatcher.HandleAsync<PlaceTerminalEquipmentInNodeContainer, Result>(placeEquipmentCmd);


            // Assert
            placeEquipmentCmdResult.IsSuccess.Should().BeTrue();
        }
        
        [Fact, Order(5)]
        public async Task PlaceSplitterInCORack1_ShouldSucceed()
        {
            // Setup
            var sutNodeContainer = TestUtilityNetwork.NodeContainer_CO_1;

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            utilityNetwork.TryGetEquipment<NodeContainer>(sutNodeContainer, out var nodeContainerBeforeCommand);

            var placeEquipmentCmd = new PlaceTerminalEquipmentInNodeContainer(
                correlationId: Guid.NewGuid(),
                userContext: new UserContext("test", Guid.Empty),
                nodeContainerId: sutNodeContainer,
                Guid.NewGuid(),
                terminalEquipmentSpecificationId: TestSpecifications.GSS_24_Splitters,
                numberOfEquipments: 1,
                startSequenceNumber: 1,
                namingMethod: TerminalEquipmentNamingMethodEnum.NameOnly,
                namingInfo: new Events.Core.Infos.NamingInfo() { Name = "24 stk 1:2 Splittere"}
            )
            {
                SubrackPlacementInfo = new SubrackPlacementInfo(nodeContainerBeforeCommand.Racks[0].Id, 40, SubrackPlacmentMethod.BottomUp)
            };


            // Act
            var placeEquipmentCmdResult = await _commandDispatcher.HandleAsync<PlaceTerminalEquipmentInNodeContainer, Result>(placeEquipmentCmd);

            var nodeContainerQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new EquipmentIdList() { placeEquipmentCmd.NodeContainerId })
            );

            var nodeContainer = nodeContainerQueryResult.Value.NodeContainers.First();


            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new EquipmentIdList(nodeContainer.Racks[0].SubrackMounts.Select(s => s.TerminalEquipmentId)))
            );

            var firstMount = nodeContainer.Racks[0].SubrackMounts[40];
            var sutEquipment = equipmentQueryResult.Value.TerminalEquipment[firstMount.TerminalEquipmentId];


            // Assert
            placeEquipmentCmdResult.IsSuccess.Should().BeTrue();
            nodeContainerQueryResult.IsSuccess.Should().BeTrue();
            equipmentQueryResult.IsSuccess.Should().BeTrue();

            // Try simple trace of splitter 1 terminal 1 (in port) - should return nothing
            var splitter1terminal1traceResult = utilityNetwork.Graph.SimpleTrace(sutEquipment.TerminalStructures.First().Terminals.First().Id);
            splitter1terminal1traceResult.Upstream.Length.Should().Be(0);
            splitter1terminal1traceResult.Downstream.Length.Should().Be(0);


            // Try advanced trace of splitter 1 terminal 1 (in port) - should return the two output terminals downstream
            var splitter1terminal1advancedTraceResult = utilityNetwork.Graph.AdvancedTrace(sutEquipment.TerminalStructures.First().Terminals.First().Id);
            //splitter1terminal1advancedTraceResult.Upstream.Length.Should().Be(0);
            //splitter1terminal1advancedTraceResult.Downstream.Length.Should().Be(4);



        }
    }
}

#nullable enable
