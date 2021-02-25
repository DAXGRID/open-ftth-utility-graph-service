using OpenFTTH.Core;
using System;
using System.Collections.Immutable;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record SpanSegmentTrace : IIdentifiedObject
    {
        public Guid SpanSegmentId { get; }

        public NodeTrace? NodeTrace { get; init; }

        public ImmutableArray<SegmentTrace>? SegmentTrace { get; init; }

        public Guid Id => SpanSegmentId;

        public string? Name => null;

        public string? Description => null;

        public SpanSegmentTrace(Guid spanSegmentId)
        {
            SpanSegmentId = spanSegmentId;
        }
    }
}
