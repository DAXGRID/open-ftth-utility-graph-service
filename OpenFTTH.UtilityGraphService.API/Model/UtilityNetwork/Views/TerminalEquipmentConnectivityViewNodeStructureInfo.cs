using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record TerminalEquipmentConnectivityViewNodeStructureInfo
    {
        public Guid Id { get; init; }
        public string Category { get; init; }
        public string Name { get; init; }
        public string SpecName { get; init; }
        public string? Info { get; init; }

        public TerminalEquipmentConnectivityViewNodeStructureInfo(Guid id, string category, string name, string specName)
        {
            Id = id;
            Category = category;
            Name = name;
            SpecName = specName;
        }
    }
}
