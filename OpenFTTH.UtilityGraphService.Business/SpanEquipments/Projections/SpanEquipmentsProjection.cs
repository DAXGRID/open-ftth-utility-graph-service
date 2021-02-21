using FluentResults;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections
{
    public class SpanEquipmentsProjection : ProjectionBase
    {
        private readonly LookupCollection<SpanEquipment> _spanEquipments = new LookupCollection<SpanEquipment>();

        public LookupCollection<SpanEquipment> SpanEquipments => _spanEquipments;

        public SpanEquipmentsProjection()
        {
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
        }

        public Result<SpanEquipment> GetEquipment(Guid spanEquipmentId)
        {
            if (_spanEquipments.TryGetValue(spanEquipmentId, out SpanEquipment? spanEquipment))
            {
                return Result.Ok<SpanEquipment>(spanEquipment);
            }
            else
            {
                return Result.Fail<SpanEquipment>($"No span equipment with id: {spanEquipmentId} found");
            }
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanEquipmentPlacedInRouteNetwork @event):
                    _spanEquipments.Add(@event.Equipment);
                    break;
            }
        }
    }
}
