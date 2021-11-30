using OpenFTTH.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record NodeContainerTerminalEquipmentReferenceAdded : EventStoreBaseEvent
    {
        public Guid NodeContainerId { get; }
        public Guid TerminalEquipmentId { get; }

        public NodeContainerTerminalEquipmentReferenceAdded(Guid nodeContainerId, Guid terminalEquipmentId)
        {
            NodeContainerId = nodeContainerId;
            TerminalEquipmentId = terminalEquipmentId;
        }
    }
}
