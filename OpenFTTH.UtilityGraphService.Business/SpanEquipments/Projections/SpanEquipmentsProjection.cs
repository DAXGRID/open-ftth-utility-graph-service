using FluentResults;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Concurrent;
using DAX.EventProcessing;
using OpenFTTH.Events.Changes;
using System.Collections.Generic;
using OpenFTTH.Events.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections
{
    public class SpanEquipmentsProjection : ProjectionBase
    {
        private readonly LookupCollection<SpanEquipment> _spanEquipmentByEquipmentId = new LookupCollection<SpanEquipment>();
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByInterestId = new ConcurrentDictionary<Guid, SpanEquipment>();

        public LookupCollection<SpanEquipment> SpanEquipments => _spanEquipmentByEquipmentId;

        public SpanEquipmentsProjection(IExternalEventProducer externalEventProducer)
        {
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
        }

        public Result<SpanEquipment> GetEquipment(Guid spanEquipmentOrInterestId)
        {
            if (_spanEquipmentByEquipmentId.TryGetValue(spanEquipmentOrInterestId, out SpanEquipment? spanEquipmentByEquipmentId))
            {
                return Result.Ok<SpanEquipment>(spanEquipmentByEquipmentId);
            }
            else if (_spanEquipmentByInterestId.TryGetValue(spanEquipmentOrInterestId, out SpanEquipment? spanEquipmentByInterestId))
            {
                return Result.Ok<SpanEquipment>(spanEquipmentByInterestId);
            }
            else
            {
                return Result.Fail<SpanEquipment>($"No span equipment with id or interest id: {spanEquipmentOrInterestId} found");
            }
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanEquipmentPlacedInRouteNetwork @event):
                    _spanEquipmentByEquipmentId.Add(@event.Equipment);
                    _spanEquipmentByInterestId.TryAdd(@event.Equipment.WalkOfInterestId, @event.Equipment);
                    break;
            }
        }
    }
}
