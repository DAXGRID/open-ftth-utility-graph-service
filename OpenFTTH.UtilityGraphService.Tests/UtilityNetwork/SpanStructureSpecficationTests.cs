using CSharpFunctionalExtensions;
using FluentAssertions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections;
using System;
using Xunit;

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    public class SpanStructuretSpecificationTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public SpanStructuretSpecificationTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [Fact]
        public async void AddSpanStructureSpecificationTest()
        {
            // Setup
            var spec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø12", "Red")
            {
                OuterDiameter = 12,
                InnerDiameter = 10
            };

            var spec2 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø50", "Red")
            {
                OuterDiameter = 50,
                InnerDiameter = 45
            };

            // Act
            var cmd1 = new AddSpanStructureSpecification(spec1);
            Result cmd1Result = await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(cmd1);

            var cmd2 = new AddSpanStructureSpecification(spec2);
            Result cmd2Result = await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(cmd2);

            // Assert
            cmd1Result.IsSuccess.Should().BeTrue();
            cmd2Result.IsSuccess.Should().BeTrue();

            var spanStructureSpecificationsProjection = _eventStore.Projections.Get<SpanStructureSpecificationsProjection>();

            spanStructureSpecificationsProjection.Specifications.TryGetValue(spec1.Id, out var _).Should().BeTrue();
            spanStructureSpecificationsProjection.Specifications.TryGetValue(spec2.Id, out var _).Should().BeTrue();
            spanStructureSpecificationsProjection.Specifications[spec1.Id].Should().BeEquivalentTo(spec1);
            spanStructureSpecificationsProjection.Specifications[spec2.Id].Should().BeEquivalentTo(spec2);
        }

        [Fact]
        public async void DepecateSpanStructureSpecificationTest()
        {
            // Setup
            var spec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø12", "Red")
            {
                OuterDiameter = 12,
                InnerDiameter = 10
            };

            var spec2 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø50", "Red")
            {
                OuterDiameter = 50,
                InnerDiameter = 45
            };

            // Act
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(spec1));
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(spec2));
            await _commandDispatcher.HandleAsync<DeprecateSpanStructureSpecification, Result>(new DeprecateSpanStructureSpecification(spec2.Id));


            // Assert
            var spanStructureSpecificationsProjection = _eventStore.Projections.Get<SpanStructureSpecificationsProjection>();

            spanStructureSpecificationsProjection.Specifications[spec1.Id].Deprecated.Should().BeFalse();
            spanStructureSpecificationsProjection.Specifications[spec2.Id].Deprecated.Should().BeTrue();
        }
    }
}
