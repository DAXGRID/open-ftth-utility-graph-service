using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipmentNodeContainerAffix
    {
        public Guid NodeContainerId { get; }
        public NodeContainerSideEnum NodeContainerIngoingSide { get; }

        public SpanEquipmentNodeContainerAffix(Guid nodeContainerId, NodeContainerSideEnum nodeContainerIngoingSide)
        {
            NodeContainerId = nodeContainerId;
            NodeContainerIngoingSide = nodeContainerIngoingSide;
        }
    }
}
