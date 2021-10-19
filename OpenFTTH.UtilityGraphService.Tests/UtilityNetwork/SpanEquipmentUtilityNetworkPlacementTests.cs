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
using Xunit.Extensions.Ordering;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.RouteNetwork.API.Queries;

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(4000)]
    public class T4000_SpanEquipmentUtilityNetworkPlacementTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public T4000_SpanEquipmentUtilityNetworkPlacementTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void TestPlaceSpanEquipmentUsingOneHopFromCO1ToCC1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var routeThroughSpanEquipmentId = TestUtilityNetwork.MultiConduit_5x10_CO_1_to_HH_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(routeThroughSpanEquipmentId, out var routeThoughSpanEquipment);

            // Setup
            var specs = new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();

            var routingHops = new RoutingHop[]
            {
                new RoutingHop(TestRouteNetwork.CO_1, routeThoughSpanEquipment.SpanStructures[5].SpanSegments[0].Id)
            };

            var placeSpanEquipmentCommand = new PlaceSpanEquipmentInUtilityNetwork(Guid.NewGuid(), new UserContext("test", Guid.Empty), Guid.NewGuid(), TestSpecifications.Multi_Ø32_3x10, routingHops)
            {
                NamingInfo = new NamingInfo("Hans", "Grethe"),
                MarkingInfo = new MarkingInfo() { MarkingColor = "Red", MarkingText = "ABCDE" },
                ManufacturerId = Guid.NewGuid()
            };

            // Act
            var placeSpanEquipmentResult = await _commandDispatcher.HandleAsync<PlaceSpanEquipmentInUtilityNetwork, Result>(placeSpanEquipmentCommand);

            utilityNetwork.TryGetEquipment<SpanEquipment>(placeSpanEquipmentCommand.SpanEquipmentId, out var placedSpanEquipment);

            var routeNetworkQueryResult = await _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
              new GetRouteNetworkDetails(new InterestIdList() { placedSpanEquipment.WalkOfInterestId })
              {
                  RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementAndInterestObjects
              }
            );

            // Assert
            placeSpanEquipmentResult.IsSuccess.Should().BeTrue();
            routeNetworkQueryResult.IsSuccess.Should().BeTrue();

            var walkOfInterest = routeNetworkQueryResult.Value.Interests[placedSpanEquipment.WalkOfInterestId];

            walkOfInterest.RouteNetworkElementRefs.First().Should().Be(TestRouteNetwork.CO_1);
            walkOfInterest.RouteNetworkElementRefs.Last().Should().Be(TestRouteNetwork.CC_1);
        }
    }
}

#nullable enable
