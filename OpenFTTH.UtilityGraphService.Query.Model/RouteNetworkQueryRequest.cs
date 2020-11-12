using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.QueryModel
{
    public record RouteNetworkQueryRequest
    {
        public RouteNetworkElementTypeEnum RouteNetworkElementType { get; }
        public Guid RouteNetworkElementId { get; }
        public bool IncludeGeometry { get; init; }
        public bool IncludeAddresses { get; init; }

        public RouteNetworkQueryRequest(RouteNetworkElementTypeEnum routeNetworkElementType, Guid routeNetworkElementId)
        {
            RouteNetworkElementType = routeNetworkElementType;
            RouteNetworkElementId = routeNetworkElementId;
        }

    }

}
