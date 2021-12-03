﻿using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SubrackPlacementInfo
    {
        public Guid RackId { get; }
        public int StartUnitPosition { get; }
        public SubrackPlacmentMethod PlacementMethod { get; }

        public SubrackPlacementInfo(Guid rackId, int startUnitPosition, SubrackPlacmentMethod placmentMethod)
        {
            RackId = rackId;
            StartUnitPosition = startUnitPosition;
            PlacementMethod = placmentMethod;
        }
    }
}
