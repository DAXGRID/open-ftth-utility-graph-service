using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Trace
{
    public record SpanSegmentTraceResult
    {
        public Guid SpanSegmentId { get; init; }
    }
}
