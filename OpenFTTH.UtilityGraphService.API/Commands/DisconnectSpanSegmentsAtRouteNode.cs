using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record DisconnectSpanSegmentsAtRouteNode : ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid[] SpanSegmentsToDisconnect { get; }

        public DisconnectSpanSegmentsAtRouteNode(Guid routeNodeId, Guid[] spanSegmentsToDisconnect)
        {
            RouteNodeId = routeNodeId;
            SpanSegmentsToDisconnect = spanSegmentsToDisconnect;
        }
    }
}
