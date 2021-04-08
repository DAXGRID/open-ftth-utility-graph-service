using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentMerged
    {
        public Guid SpanEquipmentId { get; }
        public Guid[] NodesOfInterestIds { get; }

        public SpanEquipmentMerged(Guid spanEquipmentId, Guid[] nodesOfInterestIds)
        {
            SpanEquipmentId = spanEquipmentId;
            NodesOfInterestIds = nodesOfInterestIds;
        }
    }
}
