using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.TestData;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Queries;
using System;
using Xunit;

#nullable disable

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
            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();

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

        [Fact]
        public async void TestPlacingMultipleNodeContainerInSameNode_ShouldFail()
        {
            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();

            // First node container
            var registerNodeOfInterestCommand1 = new RegisterNodeOfInterest(Guid.NewGuid(), TestRouteNetwork.HH_11);
            var registerNodeOfInterestCommandResult1 = _commandDispatcher.HandleAsync<RegisterNodeOfInterest, Result<RouteNetworkInterest>>(registerNodeOfInterestCommand1).Result;

            var placeNodeContainerCommand1 = new PlaceNodeContainerInRouteNetwork(Guid.NewGuid(), TestSpecifications.Conduit_Closure_Emtelle_Branch_Box, registerNodeOfInterestCommandResult1.Value)
            {
                ManufacturerId = TestSpecifications.Manu_Emtelle
            };

            var firstNodeContainerResult = await _commandDispatcher.HandleAsync<PlaceNodeContainerInRouteNetwork, Result>(placeNodeContainerCommand1);

            // First node container
            var registerNodeOfInterestCommand2 = new RegisterNodeOfInterest(Guid.NewGuid(), TestRouteNetwork.HH_11);
            var registerNodeOfInterestCommandResult2 = _commandDispatcher.HandleAsync<RegisterNodeOfInterest, Result<RouteNetworkInterest>>(registerNodeOfInterestCommand2).Result;

            var placeNodeContainerCommand2 = new PlaceNodeContainerInRouteNetwork(Guid.NewGuid(), TestSpecifications.Conduit_Closure_Emtelle_Branch_Box, registerNodeOfInterestCommandResult2.Value)
            {
                ManufacturerId = TestSpecifications.Manu_Emtelle
            };

            var secondNodeContainerResult = await _commandDispatcher.HandleAsync<PlaceNodeContainerInRouteNetwork, Result>(placeNodeContainerCommand2);


            // Assert
            firstNodeContainerResult.IsSuccess.Should().BeTrue();
            secondNodeContainerResult.IsSuccess.Should().BeFalse();

        }

    }
}

#nullable enable
