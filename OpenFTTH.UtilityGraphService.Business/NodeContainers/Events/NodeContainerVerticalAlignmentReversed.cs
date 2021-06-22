using OpenFTTH.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record NodeContainerVerticalAlignmentReversed : EventStoreBaseEvent
    {
        public Guid NodeContainerId { get; }

        public NodeContainerVerticalAlignmentReversed(Guid nodeContainerId)
        {
            NodeContainerId = nodeContainerId;
        }
    }
}
