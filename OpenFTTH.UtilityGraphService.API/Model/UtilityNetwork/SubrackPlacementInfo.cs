using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SubrackPlacementInfo
    {
        public Guid RackId { get; }
        public int StartUnitPosition { get; }
        public SubrackPlacmentMethod PlacmentMethod { get; }
    }
}
