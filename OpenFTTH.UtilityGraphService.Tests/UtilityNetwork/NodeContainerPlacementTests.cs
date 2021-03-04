using OpenFTTH.CQRS;
using Xunit;
using FluentAssertions;
using FluentResults;
using OpenFTTH.RouteNetwork.API.Model;
using System;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.Tests.TestData;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.Events.Core.Infos;
using DAX.EventProcessing;
using System.Linq;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.TestData;

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    public class NodeContainerPlacementTests
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public NodeContainerPlacementTests(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;
        }

        
        [Fact]
        public async void TestPlaceValidNodeContainer_ShouldSucceed()
        {
            var specs = new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();

            var nodeOfInterestId = Guid.NewGuid();
            var registerNodeOfInterestCommand = new RegisterNodeOfInterest(nodeOfInterestId, TestRouteNetwork.CC_1);
            var registerNodeOfInterestCommandResult = _commandDispatcher.HandleAsync<RegisterNodeOfInterest, Result<RouteNetworkInterest>>(registerNodeOfInterestCommand).Result;

            var placeNodeContainerCommand = new PlaceNodeContainerInRouteNetwork(Guid.NewGuid(), TestSpecifications.Conduit_Closure_Emtelle_Branch_Box, registerNodeOfInterestCommandResult.Value)
            {
                ManufacturerId = TestSpecifications.Manu_Emtelle
            };

            // Act
            var placeNodeContainerResult = await _commandDispatcher.HandleAsync<PlaceNodeContainerInRouteNetwork, Result>(placeNodeContainerCommand);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new InterestIdList() { nodeOfInterestId })
            );

            // Assert
            placeNodeContainerResult.IsSuccess.Should().BeTrue();
            equipmentQueryResult.IsSuccess.Should().BeTrue();

            equipmentQueryResult.Value.NodeContainers[placeNodeContainerCommand.NodeContainerId].Id.Should().Be(placeNodeContainerCommand.NodeContainerId);
            equipmentQueryResult.Value.NodeContainers[placeNodeContainerCommand.NodeContainerId].SpecificationId.Should().Be(placeNodeContainerCommand.NodeContainerSpecificationId);
            equipmentQueryResult.Value.NodeContainers[placeNodeContainerCommand.NodeContainerId].ManufacturerId.Should().Be(placeNodeContainerCommand.ManufacturerId);
            equipmentQueryResult.Value.NodeContainers[placeNodeContainerCommand.NodeContainerId].InterestId.Should().Be(nodeOfInterestId);

        }
    }
}
