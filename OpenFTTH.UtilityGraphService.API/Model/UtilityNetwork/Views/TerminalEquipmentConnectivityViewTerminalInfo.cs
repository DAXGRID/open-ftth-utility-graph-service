using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record TerminalEquipmentConnectivityViewTerminalInfo
    {
        public Guid Id { get; init; }
        public string Name { get; init; }

        public TerminalEquipmentConnectivityViewTerminalInfo(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
