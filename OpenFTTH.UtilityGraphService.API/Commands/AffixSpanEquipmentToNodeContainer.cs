using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AffixSpanEquipmentToNodeContainer : ICommand<Result>
    {
        public Guid SpanSegmentId { get; }
        public Guid NodeContainerId { get; }
        public NodeContainerSideEnum NodeContainerIngoingSide { get; }

        public AffixSpanEquipmentToNodeContainer(Guid spanSegmentId, Guid nodeContainerId, NodeContainerSideEnum nodeContainerIngoingSide)
        {
            SpanSegmentId = spanSegmentId;
            NodeContainerId = nodeContainerId;
            NodeContainerIngoingSide = nodeContainerIngoingSide;
        }
    }
}
