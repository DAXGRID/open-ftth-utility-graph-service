using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record MarkingInfoUpdated
    {
        public Guid SpanEquipmentId { get; }

        public MarkingInfo? MarkingInfo { get; }

        public MarkingInfoUpdated(Guid spanEquipmentId, MarkingInfo? markingInfo)
        {
            SpanEquipmentId = spanEquipmentId;
            MarkingInfo = markingInfo;
        }
    }
}
