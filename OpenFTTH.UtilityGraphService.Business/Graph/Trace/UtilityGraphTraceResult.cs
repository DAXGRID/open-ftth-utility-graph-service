using DAX.ObjectVersioning.Graph;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Trace
{
    public record UtilityGraphTraceResult
    {
        public Guid TerminalOrSpanSegmentId { get; init; }

        public IGraphObject[] Downstream = Array.Empty<IGraphObject>();
        public IGraphObject[] Upstream = Array.Empty<IGraphObject>();
    }
}
