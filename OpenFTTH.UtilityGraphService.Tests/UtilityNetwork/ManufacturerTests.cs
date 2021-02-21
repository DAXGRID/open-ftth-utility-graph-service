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

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    public class ManufactuereTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public ManufactuereTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [Fact]
        public async void AddManufacturerTest_ShouldSucceed()
        {
            // Setup
            var manu1 = new Manufacturer(Guid.NewGuid(), "Manu 1");
            var manu2 = new Manufacturer(Guid.NewGuid(), "Manu 2");

            // Act
            await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu1));
            await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu2));

            var manufacturerQueryResult = await _queryDispatcher.HandleAsync<GetManufacturer, Result<LookupCollection<Manufacturer>>>(new GetManufacturer());

            // Assert
            manufacturerQueryResult.Value[manu1.Id].Should().BeEquivalentTo(manu1);
            manufacturerQueryResult.Value[manu2.Id].Should().BeEquivalentTo(manu2);
        }

        [Fact]
        public async void AddManufacturerWithEmptyName_ShouldFail()
        {
            // Setup
            var manu1 = new Manufacturer(Guid.NewGuid(),"");

            // Act
            var cmdResult = await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu1));

            // Assert
            cmdResult.IsFailure.Should().BeTrue();
        }

        [Fact]
        public async void AddTwoManufacturerWithSameName_ShouldFail()
        {
            // Setup
            var manu1 = new Manufacturer(Guid.NewGuid(), "Hans");
            var manu2 = new Manufacturer(Guid.NewGuid(), "Hans");

            // Act
            var cmdResult1 = await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu1));
            var cmdResult2 = await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu2));

            // Assert
            cmdResult1.IsFailure.Should().BeFalse();
            cmdResult2.IsFailure.Should().BeTrue();
        }

        [Fact]
        public async void AddTwoManufacturerWithSameId_ShouldFail()
        {
            // Setup
            var manu1 = new Manufacturer(Guid.NewGuid(), "Bent");
            var manu2 = new Manufacturer(manu1.Id, "Bent");

            // Act
            var cmdResult1 = await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu1));
            var cmdResult2 = await _commandDispatcher.HandleAsync<AddManufacturer, Result>(new AddManufacturer(manu2));

            // Assert
            cmdResult1.IsFailure.Should().BeFalse();
            cmdResult2.IsFailure.Should().BeTrue();
        }

    }
}
