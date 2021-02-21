using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentSpecificationDeprecated
    {
        public Guid SpanEquipmentSpecificationId { get; }

        public SpanEquipmentSpecificationDeprecated(Guid spanEquipmentSpecificationId)
        {
            SpanEquipmentSpecificationId = spanEquipmentSpecificationId;
        }
    }
}
