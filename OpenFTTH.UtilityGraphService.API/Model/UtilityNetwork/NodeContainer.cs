using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record NodeContainer : IIdentifiedObject
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        public Guid NodeOfInterest { get; init; }
        public Guid? ManufacturerId { get; init; }

        public string? Name => null;
        public string? Description => null;

        public NodeContainer(Guid id, Guid specificationId, Guid nodeOfInterest)
        {
            Id = id;
            SpecificationId = specificationId;
            NodeOfInterest = nodeOfInterest;
        }
    }
}
