using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.TestData;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(1500)]
    public class SpanEquipmentPropertiesUpdateTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public SpanEquipmentPropertiesUpdateTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void UpdateMarkingInfo_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_12x7_SDU_1_to_J_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentBeforeUpdate);

            var updateCmd = new UpdateSpanEquipmentProperties(spanEquipmentId: sutSpanEquipmentId)
            {
                MarkingInfo = new MarkingInfo() { MarkingColor = "Red", MarkingText = "Rød"}
            };

            var updateResult = await _commandDispatcher.HandleAsync<UpdateSpanEquipmentProperties, Result>(updateCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentAfterUpdate);

            // Assert
            updateResult.IsSuccess.Should().BeTrue();
            spanEquipmentAfterUpdate.MarkingInfo.Should().BeEquivalentTo(updateCmd.MarkingInfo);

            // Check if an event is published to the notification.utility-network topic having an idlist containing the span equipment id we just created
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.Category == "EquipmentModification.PropertiesUpdated" && n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutSpanEquipmentId)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(TestRouteNetwork.J_1);
        }

        [Fact, Order(2)]
        public async void UpdateMarkingInfoSetPropertyToNull_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_12x7_SDU_1_to_J_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentBeforeUpdate);

            var updateCmd = new UpdateSpanEquipmentProperties(spanEquipmentId: sutSpanEquipmentId)
            {
                MarkingInfo = new MarkingInfo() { MarkingColor = "Red", MarkingText = null }
            };

            var updateResult = await _commandDispatcher.HandleAsync<UpdateSpanEquipmentProperties, Result>(updateCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentAfterUpdate);

            // Assert
            updateResult.IsSuccess.Should().BeTrue();
            spanEquipmentAfterUpdate.MarkingInfo.Should().BeEquivalentTo(updateCmd.MarkingInfo);
        }

        [Fact, Order(10)]
        public async void UpdateManufacturer_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_12x7_SDU_1_to_J_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentBeforeUpdate);

            var updateCmd = new UpdateSpanEquipmentProperties(spanEquipmentId: sutSpanEquipmentId)
            {
                ManufacturerId = TestSpecifications.Manu_Emtelle
            };

            var updateResult = await _commandDispatcher.HandleAsync<UpdateSpanEquipmentProperties, Result>(updateCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentAfterUpdate);

            // Assert
            updateResult.IsSuccess.Should().BeTrue();
            spanEquipmentAfterUpdate.ManufacturerId.Should().Be(updateCmd.ManufacturerId);

            // Check if an event is published to the notification.utility-network topic having an idlist containing the span equipment id we just created
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.Category == "EquipmentModification.PropertiesUpdated" && n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutSpanEquipmentId)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(TestRouteNetwork.J_1);
        }

        [Fact, Order(11)]
        public async void UpdateManufacturerToGuidEmpty_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_12x7_SDU_1_to_J_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentBeforeUpdate);

            var updateCmd = new UpdateSpanEquipmentProperties(spanEquipmentId: sutSpanEquipmentId)
            {
                ManufacturerId = Guid.Empty
            };

            var updateResult = await _commandDispatcher.HandleAsync<UpdateSpanEquipmentProperties, Result>(updateCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentAfterUpdate);

            // Assert
            updateResult.IsSuccess.Should().BeTrue();
            spanEquipmentAfterUpdate.ManufacturerId.Should().Be(updateCmd.ManufacturerId);

            // Check if an event is published to the notification.utility-network topic having an idlist containing the span equipment id we just created
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.Category == "EquipmentModification.PropertiesUpdated" && n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutSpanEquipmentId)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(TestRouteNetwork.J_1);
        }



        [Fact, Order(100)]
        public async void UpdateMarkingInfoToSameAsBefore_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_12x7_SDU_1_to_J_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentBeforeUpdate);

            var updateCmd = new UpdateSpanEquipmentProperties(spanEquipmentId: sutSpanEquipmentId)
            {
                MarkingInfo = new MarkingInfo() { MarkingColor = "Red", MarkingText = null }
            };

            var updateResult = await _commandDispatcher.HandleAsync<UpdateSpanEquipmentProperties, Result>(updateCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var spanEquipmentAfterUpdate);

            // Assert
            updateResult.IsSuccess.Should().BeFalse();
        }
    }
}

#nullable enable
