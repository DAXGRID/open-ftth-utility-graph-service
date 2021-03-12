using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record ConnectSpanSegmentsAtRouteNode : ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid[] SpanSegmentsToConnect { get; }

        public ConnectSpanSegmentsAtRouteNode(Guid routeNodeId, Guid[] spanSegmentsToConnect)
        {
            RouteNodeId = routeNodeId;
            SpanSegmentsToConnect = spanSegmentsToConnect;
        }
    }
}
