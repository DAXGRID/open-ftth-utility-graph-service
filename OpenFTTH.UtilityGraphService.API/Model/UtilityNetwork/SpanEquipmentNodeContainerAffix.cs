using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipmentNodeContainerAffix
    {
        public int NodeOfInterestIndex { get; }
        public Guid NodeContainerId { get; }
        public NodeContainerSideEnum IngoingSide { get; }
        public NodeContainerSideEnum? OutgoingSide { get; init; }

        public SpanEquipmentNodeContainerAffix(int nodeOfInterestIndex, Guid nodeContainerId, NodeContainerSideEnum ingoingSide, NodeContainerSideEnum? outgoingSide = null)
        {
            NodeOfInterestIndex = nodeOfInterestIndex;
            NodeContainerId = nodeContainerId;
            IngoingSide = ingoingSide;
            OutgoingSide = outgoingSide;
        }
    }
}
