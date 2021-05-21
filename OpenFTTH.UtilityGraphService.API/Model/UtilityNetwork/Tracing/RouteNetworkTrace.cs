using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record RouteNetworkTrace : IIdentifiedObject
    {
        public Guid Id { get; }
        public Guid FromRouteNodeId { get; }
        public Guid ToRouteNodeId { get; }
        public Guid[] RouteSegmentIds { get; }

        public string? FromRouteNodeName { get; }
        public string? ToRouteNodeName { get; }

        public string[] RouteSegmentGeometries { get; }

        public string? Name => null;

        public string? Description => null;

        public RouteNetworkTrace(Guid id, Guid fromRouteNodeId, Guid toRouteNodeId, Guid[] routeSegmentIds, string? fromRouteNodeName, string? toRouteNodeName, string[] routeSegmentGeometries)
        {
            Id = id;
            FromRouteNodeId = fromRouteNodeId;
            ToRouteNodeId = toRouteNodeId;
            RouteSegmentIds = routeSegmentIds;
            FromRouteNodeName = fromRouteNodeName;
            ToRouteNodeName = toRouteNodeName;
            RouteSegmentGeometries = routeSegmentGeometries;
        }
    }
}
