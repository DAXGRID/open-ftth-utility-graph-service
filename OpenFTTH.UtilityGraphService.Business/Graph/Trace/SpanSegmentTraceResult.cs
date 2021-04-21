using DAX.ObjectVersioning.Graph;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Trace
{
    public record SpanSegmentTraceResult
    {
        public Guid SpanSegmentId { get; init; }

        public IGraphObject[] Downstream = Array.Empty<IGraphObject>();
        public IGraphObject[] Upstream = Array.Empty<IGraphObject>();
    }
}
