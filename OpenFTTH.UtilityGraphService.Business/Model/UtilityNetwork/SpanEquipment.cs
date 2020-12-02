using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// The whole thing should be treated as an immutable structure.
    /// </summary>
    public record SpanEquipment
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public List<SpanStructure>? SpanStructures{ get; init; }

        public SpanEquipment(Guid id)
        {
            Id = id;
        }
    }
}
