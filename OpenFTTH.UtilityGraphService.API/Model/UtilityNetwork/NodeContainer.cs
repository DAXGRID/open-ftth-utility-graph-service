using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record NodeContainer : IEquipment
    {
        public Guid Id { get; }
        public Guid SpecificationId { get; }
        public Guid InterestId { get; init; }
        public Guid? ManufacturerId { get; init; }

        public string? Name => null;
        public string? Description => null;

        public NodeContainer(Guid id, Guid specificationId, Guid interestId)
        {
            Id = id;
            SpecificationId = specificationId;
            InterestId = interestId;
        }
    }
}
