using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record DisconnectSpanSegmentsAtRouteNode : BaseCommand, ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid[] SpanSegmentsToDisconnect { get; }

        public DisconnectSpanSegmentsAtRouteNode(Guid routeNodeId, Guid[] spanSegmentsToDisconnect)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            RouteNodeId = routeNodeId;
            SpanSegmentsToDisconnect = spanSegmentsToDisconnect;
        }
    }
}
