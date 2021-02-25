using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record SegmentTrace
    {
        public Guid SpanSegmentId { get; set; }
        public Guid RouteSegmentId { get; set; }

        public SegmentTrace(Guid spanSegmentId, Guid routeSegmentId)
        {
            SpanSegmentId = spanSegmentId;
            RouteSegmentId = routeSegmentId;
        }
    }
}
