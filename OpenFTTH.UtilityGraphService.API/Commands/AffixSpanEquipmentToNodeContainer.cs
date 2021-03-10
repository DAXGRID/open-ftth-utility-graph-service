using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AffixSpanEquipmentToNodeContainer : ICommand<Result>
    {
        public Guid SpanEquipmentOrSegmentId { get; }
        public Guid NodeContainerId { get; }
        public NodeContainerSideEnum NodeContainerIngoingSide { get; }

        public AffixSpanEquipmentToNodeContainer(Guid spanEquipmentOrSegmentId, Guid nodeContainerId, NodeContainerSideEnum nodeContainerIngoingSide)
        {
            SpanEquipmentOrSegmentId = spanEquipmentOrSegmentId;
            NodeContainerId = nodeContainerId;
            NodeContainerIngoingSide = nodeContainerIngoingSide;
        }
    }
}
