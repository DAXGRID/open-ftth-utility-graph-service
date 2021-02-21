using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using System;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class PlaceSpanEquipmentInRouteNetworkCommandHandler : ICommandHandler<PlaceSpanEquipmentInRouteNetwork, Result>
    {
        private readonly IEventStore _eventStore;

        public PlaceSpanEquipmentInRouteNetworkCommandHandler(IEventStore eventStore)
        {
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
                command.NamingInfo,
                command.MarkingInfo
            );

            if (placeSpanEquipmentResult.IsSuccess)
            {
                _eventStore.Aggregates.Store(spanEquipmentAR);
            }

            return Task.FromResult(placeSpanEquipmentResult);
        }
    }
}

  