using CSharpFunctionalExtensions;
using FluentAssertions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using System;
using Xunit;

namespace OpenFTTH.UtilityGraphService.Tests.SpanEquipment
{ 
    public class NodeContainerSpecificationTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public NodeContainerSpecificationTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [Fact]
        public async void AddValidNodeContainerSpecification_ShouldSucceed()
        {
            // Create manufacturer
            var manufacturer = new Manufacturer(Guid.NewGuid(), "Node Container Manufacturer");
            await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manufacturer));

            // Setup a node equipment container specification
            var newNodeContainerSpecification = new NodeContainerSpecification(Guid.NewGuid(), "ManHoles", "Draka xyz")
            {
                Description = "Draka super duper xyz",
                ManufacturerRefs = new Guid[] { manufacturer.Id }
            };

            // Act
            var addNodeSpecificationCommandResult = await _commandDispatcher.HandleAsync<AddNodeContainerSpecification, Result>(new AddNodeContainerSpecification(newNodeContainerSpecification));

            var nodeContainerSpecificationsQueryResult = await _queryDispatcher.HandleAsync<GetNodeContainerSpecifications, Result<LookupCollection<NodeContainerSpecification>>>(new GetNodeContainerSpecifications());

            // Assert
            addNodeSpecificationCommandResult.IsSuccess.Should().BeTrue();
            nodeContainerSpecificationsQueryResult.IsSuccess.Should().BeTrue();
            nodeContainerSpecificationsQueryResult.Value[newNodeContainerSpecification.Id].Name.Should().Be(newNodeContainerSpecification.Name);
            nodeContainerSpecificationsQueryResult.Value[newNodeContainerSpecification.Id].Description.Should().Be(newNodeContainerSpecification.Description);
        }


    }
}
