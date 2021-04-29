using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.RouteNetwork.API.Model;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record MoveSpanEquipment : BaseCommand, ICommand<Result>
    {
        public Guid SpanEquipmentOrSegmentId { get; }
        public RouteNetworkElementIdList NewWalkIds { get; }

        public MoveSpanEquipment(Guid spanEquipmentId, RouteNetworkElementIdList newWalkIds)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            this.SpanEquipmentOrSegmentId = spanEquipmentId;
            this.NewWalkIds = newWalkIds;
        }
    }
}
