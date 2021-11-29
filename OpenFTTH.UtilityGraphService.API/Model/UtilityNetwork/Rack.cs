﻿using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record Rack
    {
        public Guid Id { get; }
        public string Name { get; }
        public int Position { get; }
        public Guid SpecificationId { get; }
        public int HeightInUnits { get; }

        public SubrackMount[] SubrackMounts { get; }

        public Rack(Guid id, string name, int position, Guid specificationId, int heightInUnits, SubrackMount[] subrackMounts)
        {
            Id = id;
            Name = name;
            Position = position;
            SpecificationId = specificationId;
            HeightInUnits = heightInUnits;
            SubrackMounts = subrackMounts;
        }
    }
}
