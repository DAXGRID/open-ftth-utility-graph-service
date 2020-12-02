using System;

namespace OpenFTTH.UtilityGraphService.QueryModel
{
    /// <summary>
    /// Used hold information as a result of a query on route node level
    /// </summary>
    public record RouteNetworkQueryResult
    {
        public RouteNodeInfo[]? RouteNodes { get; init; }

        public RouteSegmentInfo[]? RouteSegments { get; init; }

        public WalkInfo[]? Walks { get; init; }

        public SpanEquipmentInfo[]? SpanEquipments { get; init; }
    }
}
