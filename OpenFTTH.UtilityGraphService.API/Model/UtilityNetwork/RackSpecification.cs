using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record RackSpecification : IIdentifiedObject
    {
        public Guid Id { get;}
        public string Category { get; }
        public string Name { get; }
        public string ShortName { get; }
        public bool Deprecated { get; }
        public string? Description { get; init; }

        public RackSpecification(Guid id, string category, string name, string shortName, bool deprecated)
        {
            Id = id;
            Category = category;
            Name = name;
            ShortName = shortName;
            Deprecated = deprecated;
        }
    }
}
