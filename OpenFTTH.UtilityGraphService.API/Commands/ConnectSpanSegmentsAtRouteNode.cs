using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record ConnectSpanSegmentsAtRouteNode : BaseCommand, ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid[] SpanSegmentsToConnect { get; }

        public ConnectSpanSegmentsAtRouteNode(Guid routeNodeId, Guid[] spanSegmentsToConnect)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            RouteNodeId = routeNodeId;
            SpanSegmentsToConnect = spanSegmentsToConnect;
        }
    }
}
