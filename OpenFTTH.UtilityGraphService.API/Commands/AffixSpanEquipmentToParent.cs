using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AffixSpanEquipmentToParent : BaseCommand, ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid SpanSegmentId1 { get; }
        public Guid SpanSegmentId2 { get; }

        public AffixSpanEquipmentToParent(Guid correlationId, UserContext userContext, Guid routeNodeId, Guid spanSegmentId1, Guid spanSegmentId2) : base(correlationId, userContext)
        {
            RouteNodeId = routeNodeId;
            SpanSegmentId1 = spanSegmentId1;
            SpanSegmentId2 = spanSegmentId2;
        }
    }
}
