using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record InterfaceInfo
    {
        public string InterfaceType { get; }
        public int SlotNumber { get; }
        public int SubSlotNumber { get; }
        public int PortNumber { get; }
        public string CircuitName { get; }

        public InterfaceInfo(string interfaceType, int slotNumber, int subSlotNumber, int portNumber, string circuitName)
        {
            InterfaceType = interfaceType;
            SlotNumber = slotNumber;
            SubSlotNumber = subSlotNumber;
            PortNumber = portNumber;
            CircuitName = circuitName;
        }
    }
}
