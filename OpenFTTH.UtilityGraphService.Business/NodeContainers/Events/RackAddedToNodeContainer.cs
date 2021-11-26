using OpenFTTH.Events;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record RackAddedToNodeContainer : EventStoreBaseEvent
    {
        public Guid NodeContainerId { get; }
        public Guid RackSpecificationId { get; }
        public string RackName { get;}
        public int RackPosition { get; }

        public RackAddedToNodeContainer(Guid nodeContainerId, Guid rackSpecificationId, string rackName, int rackPosition)
        {
            NodeContainerId = nodeContainerId;
            RackSpecificationId = rackSpecificationId;
            RackName = rackName;
            RackPosition = rackPosition;
        }
    }
}
