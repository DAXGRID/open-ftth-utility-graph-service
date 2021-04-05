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

        public string? Name => null;

        public string? Description => null;

        public RouteNetworkTrace(Guid id, Guid fromRouteNodeId, Guid toRouteNodeId, Guid[] routeSegmentIds)
        {
            Id = id;
            FromRouteNodeId = fromRouteNodeId;
            ToRouteNodeId = toRouteNodeId;
            RouteSegmentIds = routeSegmentIds;
        }
    }
}
