﻿using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentMoved
    {
        public Guid SpanEquipmentId { get; }
        public Guid[] NodesOfInterestIds { get; }

        public SpanEquipmentMoved(Guid spanEquipmentId, Guid[] nodesOfInterestIds)
        {
            SpanEquipmentId = spanEquipmentId;
            NodesOfInterestIds = nodesOfInterestIds;
        }
    }
}
