using CSharpFunctionalExtensions;
using FluentAssertions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.API.Util;
using System;
using Xunit;

namespace OpenFTTH.UtilityGraphService.Tests.SpanEquipment
{ 
    public class SpanEquipmentSpecificationTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public SpanEquipmentSpecificationTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [Fact]
        public async void AddValidMultiLevelSpanEquipmentSpecification_ShouldSucceed()
        {
            // Setup some span structure specifications to be used in the span equipment specification
            var outerConduitSpanStructureSpec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø50", "Orange")
            {
                OuterDiameter = 50,
                InnerDiameter = 45
            };
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(outerConduitSpanStructureSpec1));

            var innerConduitSpanStructureSpec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø12/10", "Red")
            {
                OuterDiameter = 12,
                InnerDiameter = 10
            };
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(innerConduitSpanStructureSpec1));

            var innerConduitSpanStructureSpec2 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø12/10", "Blue")
            {
                OuterDiameter = 12,
                InnerDiameter = 10
            };
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(innerConduitSpanStructureSpec2));


            // Setup a span equipment specification with 2 levels
            var spanEquipmentSpecification = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 2x12",
                new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 1, 1,
                    new SpanStructureTemplate[] {
                    }
                ));

            // Act
            var addSpanEquipmentSpecificationCommandResult = await _commandDispatcher.HandleAsync<AddSpanEquipmentSpecification, Result>(new AddSpanEquipmentSpecification(spanEquipmentSpecification));

            var spanEqipmentSpecificationsQueryResult = await _queryDispatcher.HandleAsync<GetSpanEquipmentSpecifications, Result<LookupCollection<SpanEquipmentSpecification>>>(new GetSpanEquipmentSpecifications());


            // Assert
            addSpanEquipmentSpecificationCommandResult.IsSuccess.Should().BeTrue();
            spanEqipmentSpecificationsQueryResult.IsSuccess.Should().BeTrue();

        }

        [Fact]
        public async void AddInvalidSpanEquipmentSpecificationWithRootTemplateLevelDifferentFromOne_ShouldFail()
        {
            // Setup some span structure specifications to be used in the span equipment specification
            var outerConduitSpanStructureSpec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø50", "Orange")
            {
                OuterDiameter = 50,
                InnerDiameter = 45
            };
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(outerConduitSpanStructureSpec1));


            // Setup a span equipment specification with level 0 in root span template. 
            // Must fail, because we want the root template to always have level 1
            var spanEquipmentSpecification = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 2x12",
                new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 0, 1,
                    new SpanStructureTemplate[] {
                    }
                ));

            // Act
            var addSpanEquipmentSpecificationCommandResult = await _commandDispatcher.HandleAsync<AddSpanEquipmentSpecification, Result>(new AddSpanEquipmentSpecification(spanEquipmentSpecification));

            // Assert
            addSpanEquipmentSpecificationCommandResult.IsFailure.Should().BeTrue();

        }

        [Fact]
        public async void AddInvalidSpanEquipmentSpecificationWithWrongChildTemplateLevel_ShouldFail()
        {
            // Setup some span structure specifications to be used in the span equipment specification
            var outerConduitSpanStructureSpec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø50", "Orange")
            {
                OuterDiameter = 50,
                InnerDiameter = 45
            };
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(outerConduitSpanStructureSpec1));

            // Add span equipment specification with child templates as level 2 set to level 3
            var spanEquipmentSpecification = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 2x12",
                new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 1, 1,
                    new SpanStructureTemplate[] {
                        new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 3, 1, Array.Empty<SpanStructureTemplate>())
                    }
                ));

            // Act
            var addSpanEquipmentSpecificationCommandResult = await _commandDispatcher.HandleAsync<AddSpanEquipmentSpecification, Result>(new AddSpanEquipmentSpecification(spanEquipmentSpecification));

            // Assert
            addSpanEquipmentSpecificationCommandResult.IsFailure.Should().BeTrue();

        }

        [Fact]
        public async void AddInvalidSpanEquipmentSpecificationWithNonUniqueLevelAndPosition_ShouldFail()
        {
            // Setup some span structure specifications to be used in the span equipment specification
            var outerConduitSpanStructureSpec1 = new SpanStructureSpecification(Guid.NewGuid(), "Conduit", "Ø50", "Orange")
            {
                OuterDiameter = 50,
                InnerDiameter = 45
            };
            await _commandDispatcher.HandleAsync<AddSpanStructureSpecification, Result>(new AddSpanStructureSpecification(outerConduitSpanStructureSpec1));

            // Add span equipment specification with two child template having same level and position
            var spanEquipmentSpecification = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 2x12",
                new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 1, 1,
                    new SpanStructureTemplate[] {
                        new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 2, 1, Array.Empty<SpanStructureTemplate>()),
                        new SpanStructureTemplate(outerConduitSpanStructureSpec1.Id, 2, 1, Array.Empty<SpanStructureTemplate>())
                    }
                ));

            // Act
            var addSpanEquipmentSpecificationCommandResult = await _commandDispatcher.HandleAsync<AddSpanEquipmentSpecification, Result>(new AddSpanEquipmentSpecification(spanEquipmentSpecification));

            // Assert
            addSpanEquipmentSpecificationCommandResult.IsFailure.Should().BeTrue();

        }

        [Fact]
        public async void AddInvalidSpanEquipmentSpecificationWithNonExistingStructureSpecification_ShouldFail()
        {
            // Setup
            var spanEquipmentIdThatDontExist = Guid.NewGuid();

            var spanEquipmentSpecification = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 2x12",
                new SpanStructureTemplate(spanEquipmentIdThatDontExist, 1, 1, Array.Empty<SpanStructureTemplate>()
                ));

            // Act
            var addSpanEquipmentSpecificationCommandResult = await _commandDispatcher.HandleAsync<AddSpanEquipmentSpecification, Result>(new AddSpanEquipmentSpecification(spanEquipmentSpecification));

            // Assert
            addSpanEquipmentSpecificationCommandResult.IsFailure.Should().BeTrue();

        }


    }
}
