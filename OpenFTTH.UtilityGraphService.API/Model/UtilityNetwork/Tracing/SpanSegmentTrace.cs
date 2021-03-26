using OpenFTTH.Core;
using System;
using System.Collections.Immutable;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record SpanSegmentTrace : IIdentifiedObject
    {
        public Guid SpanSegmentId { get; }
        public SegmentTrace[]? Downstream { get; }
        public SegmentTrace[]? Upstream { get; }
        public Guid Id => SpanSegmentId;
        public string? Name => null;
        public string? Description => null;

        public SpanSegmentTrace(Guid spanSegmentId, SegmentTrace[]? downstream, SegmentTrace[]? upstream)
        {
            SpanSegmentId = spanSegmentId;
            Downstream = downstream;
            Upstream = upstream;
        }
    }
}
