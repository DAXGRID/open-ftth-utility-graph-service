using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    /// <summary>
    /// Immutable structure holding a span structure and its segments. 
    /// Please keep this structure as light as possible, as millions of these guys will exchanged and cached in memory.
    /// </summary>
    public record SpanStructure : IIdentifiedObject
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        public SpanSegment[] SpanSegments { get; }

        public string? Name => this.GetType().Name;
        public string? Description => null;

        public SpanStructure(Guid id, Guid specificationId, SpanSegment[] spanSegments)
        {
            this.Id = id;
            this.SpecificationId = specificationId;
            this.SpanSegments = spanSegments;
        }
    }
}
