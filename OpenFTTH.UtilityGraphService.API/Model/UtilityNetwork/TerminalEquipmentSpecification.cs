using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record TerminalEquipmentSpecification : IIdentifiedObject
    {
        public Guid Id { get; }
        public string Category { get;}
        public string Name { get; }
        public string ShortName { get; }
        public bool IsRackEquipment { get; }
        public TerminalStructureTemplate[] StructureTemplates { get; }
        public bool Deprecated { get; init; }
        public bool IsFixed { get; init; }
        public string? Description { get; init; }
        public Guid[]? ManufacturerRefs { get; init; }

        public TerminalEquipmentSpecification(Guid id, string category, string name, string shortName, bool isRackEquipment, TerminalStructureTemplate[] structureTemplates)
        {
            Id = id;
            Category = category;
            Name = name;
            ShortName = shortName;
            IsRackEquipment = isRackEquipment;
            StructureTemplates = structureTemplates;
        }
    }
}

