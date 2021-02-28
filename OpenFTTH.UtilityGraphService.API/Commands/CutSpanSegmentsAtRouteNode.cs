using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record CutSpanSegmentsAtRouteNode : ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid SpanEquipmentId { get; }
        public Guid[] SpanSegmentsToCut { get; }

        public CutSpanSegmentsAtRouteNode(Guid routeNodeId, Guid spanEquipmentId, Guid[] spanSegmentsToCut)
        {
            RouteNodeId = routeNodeId;
            SpanEquipmentId = spanEquipmentId;
            SpanSegmentsToCut = spanSegmentsToCut;
        }
    }
}
