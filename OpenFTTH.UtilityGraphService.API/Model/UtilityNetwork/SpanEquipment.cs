using OpenFTTH.Events.Core.Infos;
using System;
using System.Collections.Immutable;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipment 
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        public WalkInfo WalkInfo { get; }
        public ImmutableArray<SpanStructure> SpanStructures { get; }

        public NamingInfo? NamingInfo { get; init; }
        public MarkingInfo? MarkingInfo { get; init; }

        public SpanEquipment(Guid id, Guid specificationId, WalkInfo walkInfo, SpanStructure[] spanStructures)
        {
            this.Id = id;
            this.SpecificationId = specificationId;
            this.WalkInfo = walkInfo;
            this.SpanStructures = ImmutableArray.Create(spanStructures);
        }
    }
}
