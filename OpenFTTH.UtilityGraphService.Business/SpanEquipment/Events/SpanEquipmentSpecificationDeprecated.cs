using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events
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
