using OpenFTTH.Core;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.RouteNetwork.API.Model;
using System;
using System.Collections.Immutable;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipment : IIdentifiedObject
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        //public RouteNetworkInterest WalkOfInterest { get; }
        public Guid WalkOfInterestId { get; }
        public Guid[] NodesOfInterestIds { get; }
        public ImmutableArray<SpanStructure> SpanStructures { get; }

        public Guid? ManufacturerId { get; init; }
        public NamingInfo? NamingInfo { get; init; }
        public MarkingInfo? MarkingInfo { get; init; }
        
        public string? Name => NamingInfo?.Name;
        public string? Description => NamingInfo?.Description;

        public SpanEquipment(Guid id, Guid specificationId, Guid walkOfInterestId, Guid[] nodesOfInterestIds, SpanStructure[] spanStructures)
        {
            this.Id = id;
            this.SpecificationId = specificationId;
            this.WalkOfInterestId = walkOfInterestId;
            this.NodesOfInterestIds = nodesOfInterestIds;
            this.SpanStructures = ImmutableArray.Create(spanStructures);
        }
    }
}
