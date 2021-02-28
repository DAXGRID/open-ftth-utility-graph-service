using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class PlaceSpanEquipmentInRouteNetworkCommandHandler : ICommandHandler<PlaceSpanEquipmentInRouteNetwork, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IExternalEventProducer _externalEventProducer;

        public PlaceSpanEquipmentInRouteNetworkCommandHandler(IEventStore eventStore, IExternalEventProducer externalEventProducer)
        {
            _externalEventProducer = externalEventProducer;
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(PlaceSpanEquipmentInRouteNetwork command)
        {
            var spanEquipments = _eventStore.Projections.Get<SpanEquipmentsProjection>().SpanEquipments;
            var spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            var spanEquipmentAR = new SpanEquipmentAR();

            var placeSpanEquipmentResult = spanEquipmentAR.PlaceSpanEquipmentInRouteNetwork(
                spanEquipments, 
                spanEquipmentSpecifications, 
                command.SpanEquipmentId,
                command.SpanEquipmentSpecificationId,
                command.Interest,
                command.ManufacturerId,
                command.NamingInfo,
                command.MarkingInfo
            );

            if (placeSpanEquipmentResult.IsSuccess)
            {
                _eventStore.Aggregates.Store(spanEquipmentAR);
                NotifyExternalServicesAboutChange(command);
            }

            return Task.FromResult(placeSpanEquipmentResult);
        }

        private async void NotifyExternalServicesAboutChange(PlaceSpanEquipmentInRouteNetwork spanEquipmentCommand)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Addition, new Guid[] { spanEquipmentCommand.SpanEquipmentId })
            };

            var updatedEvent =
                new RouteNetworkElementContainedEquipmentUpdated(
                    eventType: typeof(RouteNetworkElementContainedEquipmentUpdated).Name,
                    eventId: Guid.NewGuid(),
                    eventTimestamp: DateTime.UtcNow,
                    applicationName: "UtilityNetworkService",
                    applicationInfo: null,
                    category: "EquipmentModification",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: spanEquipmentCommand.Interest.RouteNetworkElementRefs.ToArray()
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);

        }
    }
}

  