using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SubrackPlacementInfo
    {
        public Guid RackId { get; }
        public int StartUnitPosition { get; }
        public SubrackPlacmentMethod PlacmentMethod { get; }

        public SubrackPlacementInfo(Guid rackId, int startUnitPosition, SubrackPlacmentMethod placmentMethod)
        {
            RackId = rackId;
            StartUnitPosition = startUnitPosition;
            PlacmentMethod = placmentMethod;
        }
    }
}
