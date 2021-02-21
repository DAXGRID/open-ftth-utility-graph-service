using OpenFTTH.Events.Core.Infos;
using OpenFTTH.RouteNetwork.API.Model;
using System;
using System.Collections.Immutable;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipment 
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        public RouteNetworkInterest WalkOfInterest { get; }
        public ImmutableArray<SpanStructure> SpanStructures { get; }

        public NamingInfo? NamingInfo { get; init; }
        public MarkingInfo? MarkingInfo { get; init; }

        public SpanEquipment(Guid id, Guid specificationId, RouteNetworkInterest walkOfInterest, SpanStructure[] spanStructures)
        {
            this.Id = id;
            this.SpecificationId = specificationId;
            this.WalkOfInterest = walkOfInterest;
            this.SpanStructures = ImmutableArray.Create(spanStructures);
        }
    }
}
