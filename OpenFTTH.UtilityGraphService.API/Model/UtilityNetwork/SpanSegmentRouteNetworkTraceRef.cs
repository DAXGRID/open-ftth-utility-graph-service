using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanSegmentRouteNetworkTraceRef
    {
        public Guid SpanSegmentId { get; }
        public Guid RouteSegmentTraceId { get; }
    }
}
