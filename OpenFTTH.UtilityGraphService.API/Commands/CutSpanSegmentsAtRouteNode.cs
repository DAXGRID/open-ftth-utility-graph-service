using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record CutSpanSegmentsAtRouteNode : BaseCommand, ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid[] SpanSegmentsToCut { get; }

        public CutSpanSegmentsAtRouteNode(Guid routeNodeId, Guid[] spanSegmentsToCut)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            RouteNodeId = routeNodeId;
            SpanSegmentsToCut = spanSegmentsToCut;
        }
    }
}
