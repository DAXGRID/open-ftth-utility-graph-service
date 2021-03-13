using System;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record SpanEquipmentDetachedFromContainer
    {
        public Guid SpanEquipmentId { get; }
        public Guid NodeContainerId { get; }

        public SpanEquipmentDetachedFromContainer(Guid spanEquipmentId, Guid nodeContainerId)
        {
            SpanEquipmentId = spanEquipmentId;
            NodeContainerId = nodeContainerId;
        }
    }
}
