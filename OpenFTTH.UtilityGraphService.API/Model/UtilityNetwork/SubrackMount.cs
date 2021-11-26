using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SubrackMount
    {
        public int Position { get; }
        public Guid TerminalEquipmentId { get; }

        public SubrackMount(int position, Guid terminalEquipmentId)
        {
            Position = position;
            TerminalEquipmentId = terminalEquipmentId;
        }
    }
}
