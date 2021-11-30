using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    /// <summary>
    /// Immutable structure holding a terminal structure and its terminals. 
    /// Please keep this structure as light as possible, as millions of these guys will be exchanged and cached in memory.
    /// </summary>
    public record TerminalStructure : IIdentifiedObject
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        public UInt16 Position { get; }

        public string? Name => this.GetType().Name;
        public string? Description => null;

        public TerminalStructure(Guid id, Guid specificationId, ushort position)
        {
            Id = id;
            SpecificationId = specificationId;
            Position = position;
        }
    }
}
