using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentRemoved
    {
        public Guid SpanEquipmentId { get; }

        public SpanEquipmentRemoved(Guid spanEquipmentId)
        {
            SpanEquipmentId = spanEquipmentId;
        }
    }
}
