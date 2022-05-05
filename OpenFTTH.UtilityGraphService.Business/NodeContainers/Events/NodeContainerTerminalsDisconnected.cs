using OpenFTTH.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record NodeContainerTerminalsDisconnected : EventStoreBaseEvent
    {
        public Guid NodeContainerId { get; }
        public Guid FromTerminalEquipmentId { get; }
        public Guid FromTerminalId { get; }
        public Guid ToTerminalEquipmentId { get; }
        public Guid ToTerminalId { get; }
    }
}
