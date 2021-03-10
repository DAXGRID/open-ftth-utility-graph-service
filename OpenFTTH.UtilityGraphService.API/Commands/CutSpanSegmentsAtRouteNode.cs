using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record CutSpanSegmentsAtRouteNode : ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid[] SpanSegmentsToCut { get; }

        public CutSpanSegmentsAtRouteNode(Guid routeNodeId, Guid[] spanSegmentsToCut)
        {
            RouteNodeId = routeNodeId;
            SpanSegmentsToCut = spanSegmentsToCut;
        }
    }
}
