using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    /// <summary>
    /// Represents a line for displaying a hop in a connectivity trace view
    /// </summary>
    public record ConnectivityTraceViewHopInfo
    {
        public int Level { get; }
        public bool IsSplitter { get; }
        public bool IsTraceSource { get; }
        public string Node { get; }
        public string Equipment { get; }
        public string TerminalStructure { get; }
        public string Terminal { get; }
        public string ConnectionInfo { get; }
        public double TotalLength { get; }
        public Guid[] RouteSegmentIds { get; }
        public string[] RouteSegmentGeometries { get; }

        public ConnectivityTraceViewHopInfo(int level, bool isSplitter, bool isTraceSource, string node, string equipment, string terminalStructure, string terminal, string connectionInfo, double totalLength, Guid[] routeSegmentIds, string[] routeSegmentGeometries)
        {
            Level = level;
            IsSplitter = isSplitter;
            IsTraceSource = isTraceSource;
            Node = node;
            Equipment = equipment;
            TerminalStructure = terminalStructure;
            Terminal = terminal;
            ConnectionInfo = connectionInfo;
            TotalLength = totalLength;
            RouteSegmentIds = routeSegmentIds;
            RouteSegmentGeometries = routeSegmentGeometries;
        }
    }
}
