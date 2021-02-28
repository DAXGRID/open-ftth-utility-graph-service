﻿using OpenFTTH.CQRS;
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

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    public class SpanEquipmentPlacementTests
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public SpanEquipmentPlacementTests(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;
        }

        [Fact]
        public async void TestPlaceSimpleValidSpanEquipment_ShouldSucceed()
        {
            // Setup
            var conduitSpecs = new ConduitSpecificationsTestDataGenerator(_commandDispatcher, _queryDispatcher).Run();

            var walkOfInterest = new RouteNetworkInterest(Guid.NewGuid(), RouteNetworkInterestKindEnum.WalkOfInterest, new RouteNetworkElementIdList() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

            var placeSpanEquipmentCommand = new PlaceSpanEquipmentInRouteNetwork(Guid.NewGuid(), conduitSpecs.Multi_Ø32_3x10, walkOfInterest)
            {
                NamingInfo = new NamingInfo("Hans", "Grethe"),
                MarkingInfo = new MarkingInfo() { MarkingColor = "Red", MarkingText = "ABCDE" },
                ManufacturerId = Guid.NewGuid()
            };

            // Act
            var placeSpanEquipmentResult = await _commandDispatcher.HandleAsync<PlaceSpanEquipmentInRouteNetwork, Result>(placeSpanEquipmentCommand);

            var spanEquipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new EquipmentIdList() { placeSpanEquipmentCommand.SpanEquipmentId })
            );

            // Assert
            placeSpanEquipmentResult.IsSuccess.Should().BeTrue();
            spanEquipmentQueryResult.IsSuccess.Should().BeTrue();

            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].Id.Should().Be(placeSpanEquipmentCommand.SpanEquipmentId);
            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].SpecificationId.Should().Be(placeSpanEquipmentCommand.SpanEquipmentSpecificationId);
            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].WalkOfInterest.Should().BeEquivalentTo(placeSpanEquipmentCommand.Interest);
            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].NamingInfo.Should().BeEquivalentTo(placeSpanEquipmentCommand.NamingInfo);
            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].MarkingInfo.Should().BeEquivalentTo(placeSpanEquipmentCommand.MarkingInfo);
            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].ManufacturerId.Should().Be(placeSpanEquipmentCommand.ManufacturerId);

            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].SpanStructures.Length.Should().Be(4);

            // We check if there's an event in the notification.utility-network topic having an idlist containing the span equipment id we just created
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            utilityNetworkNotifications.Any(n => n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == placeSpanEquipmentCommand.SpanEquipmentId))).Should().BeTrue();
        }

        [Fact]
        public async void TestQuerySpanEquipmentByInterestId_ShouldSucceed()
        {
            // Setup
            var conduitSpecs = new ConduitSpecificationsTestDataGenerator(_commandDispatcher, _queryDispatcher).Run();

            var walkOfInterest = new RouteNetworkInterest(Guid.NewGuid(), RouteNetworkInterestKindEnum.WalkOfInterest, new RouteNetworkElementIdList() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

            var placeSpanEquipmentCommand = new PlaceSpanEquipmentInRouteNetwork(Guid.NewGuid(), conduitSpecs.Multi_Ø32_3x10, walkOfInterest);

            // Act
            var placeSpanEquipmentResult = await _commandDispatcher.HandleAsync<PlaceSpanEquipmentInRouteNetwork, Result>(placeSpanEquipmentCommand);

            var spanEquipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new InterestIdList() { walkOfInterest.Id })
            );

            // Assert
            placeSpanEquipmentResult.IsSuccess.Should().BeTrue();
            spanEquipmentQueryResult.IsSuccess.Should().BeTrue();

            spanEquipmentQueryResult.Value.SpanEquipment[placeSpanEquipmentCommand.SpanEquipmentId].Id.Should().Be(placeSpanEquipmentCommand.SpanEquipmentId);
        }

        [Fact]
        public async void TestPlaceTwoSpanEquipmentWithSameId_ShouldFail()
        {
            // Setup
            var conduitSpecs = new ConduitSpecificationsTestDataGenerator(_commandDispatcher, _queryDispatcher).Run();

            var walkOfInterest = new RouteNetworkInterest(Guid.NewGuid(), RouteNetworkInterestKindEnum.WalkOfInterest, new RouteNetworkElementIdList() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

            var placeSpanEquipmentCommand = new PlaceSpanEquipmentInRouteNetwork(Guid.NewGuid(), conduitSpecs.Multi_Ø32_3x10, walkOfInterest);

            // Act
            var placeSpanEquipmentResult = await _commandDispatcher.HandleAsync<PlaceSpanEquipmentInRouteNetwork, Result>(placeSpanEquipmentCommand);
            var placeSpanEquipmentResult2 = await _commandDispatcher.HandleAsync<PlaceSpanEquipmentInRouteNetwork, Result>(placeSpanEquipmentCommand);


            // Assert
            placeSpanEquipmentResult.IsSuccess.Should().BeTrue();
            placeSpanEquipmentResult2.IsSuccess.Should().BeFalse();


        }
    }
}
