using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentManufacturerChanged
    {
        public Guid SpanEquipmentId { get; }

        public Guid ManufacturerId { get; }

        public SpanEquipmentManufacturerChanged(Guid spanEquipmentId, Guid manufacturerId)
        {
            SpanEquipmentId = spanEquipmentId;
            ManufacturerId = manufacturerId;
        }
    }
}
