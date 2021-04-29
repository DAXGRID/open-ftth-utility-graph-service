using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AffixSpanEquipmentToNodeContainer : BaseCommand, ICommand<Result>
    {
        public Guid SpanEquipmentOrSegmentId { get; }
        public Guid NodeContainerId { get; }
        public NodeContainerSideEnum NodeContainerIngoingSide { get; }

        public AffixSpanEquipmentToNodeContainer(Guid spanEquipmentOrSegmentId, Guid nodeContainerId, NodeContainerSideEnum nodeContainerIngoingSide)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            SpanEquipmentOrSegmentId = spanEquipmentOrSegmentId;
            NodeContainerId = nodeContainerId;
            NodeContainerIngoingSide = nodeContainerIngoingSide;
        }
    }
}
