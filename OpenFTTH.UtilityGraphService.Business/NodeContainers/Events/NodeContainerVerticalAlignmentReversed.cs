using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record NodeContainerVerticalAlignmentReversed
    {
        public Guid NodeContainerId { get; }

        public NodeContainerVerticalAlignmentReversed(Guid nodeContainerId)
        {
            NodeContainerId = nodeContainerId;
        }
    }
}
