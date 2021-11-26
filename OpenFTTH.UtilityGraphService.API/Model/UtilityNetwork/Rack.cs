using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record Rack
    {
        public string Name { get; }
        public int Position { get; }
        public Guid SpecificationId { get; }
        public SubrackMount[] SubrackMounts { get; }

        public Rack(string name, int position, Guid specificationId, SubrackMount[] subrackMounts)
        {
            Name = name;
            Position = position;
            SpecificationId = specificationId;
            SubrackMounts = subrackMounts;
        }
    }
}
