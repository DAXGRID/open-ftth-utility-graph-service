using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    /// <summary>
    /// Used as part of a terminal equipment for specification specifying cards, trays etc.
    /// </summary>
    public record TerminalStructureSpecification : IIdentifiedObject
    {
        public Guid Id { get;}
        public string Category { get; }
        public string Name { get; }
        public string ShortName { get; }   
        public TerminalTemplate[] TerminalTemplates { get; }
        public bool Deprecated { get; init; }
        public string? Description { get; init; }
    }
}
