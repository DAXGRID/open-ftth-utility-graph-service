using System.Collections.Immutable;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record NodeTrace
    {
        public NodeTraceEnd From { get;  }
        public NodeTraceEnd To { get; }
        public ImmutableArray<NodeTraceIntermediate> Intermediates { get; init; }

        public NodeTrace(NodeTraceEnd from, NodeTraceEnd to)
        {
            From = from;
            To = to;
        }
    }
}
