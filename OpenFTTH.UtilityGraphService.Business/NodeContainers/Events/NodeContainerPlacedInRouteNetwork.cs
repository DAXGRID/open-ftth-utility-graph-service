﻿using OpenFTTH.Events;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record NodeContainerPlacedInRouteNetwork : EventStoreBaseEvent
    {
        public NodeContainer Container { get; }

        public NodeContainerPlacedInRouteNetwork(NodeContainer container)
        {
            this.Container = container;
        }
    }
}
