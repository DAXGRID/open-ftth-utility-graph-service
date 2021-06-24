using OpenFTTH.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentCutReverted : EventStoreBaseEvent
    {
        public Guid SpanEquipmentId { get; }
        public Guid CutNodeOfInterestId { get; }

        public SpanEquipmentCutReverted(Guid spanEquipmentId, Guid cutNodeOfInterestId)
        {
            SpanEquipmentId = spanEquipmentId;
            CutNodeOfInterestId = cutNodeOfInterestId;
        }
    }
}
