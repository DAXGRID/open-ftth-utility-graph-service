using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record DetachSpanEquipmentFromNodeContainer : ICommand<Result>
    {
        public Guid SpanEquipmentOrSegmentId { get; }
        public Guid NodeContainerId { get; }

        public DetachSpanEquipmentFromNodeContainer(Guid spanEquipmentOrSegmentId, Guid nodeContainerId)
        {
            SpanEquipmentOrSegmentId = spanEquipmentOrSegmentId;
            NodeContainerId = nodeContainerId;
        }
    }
}
