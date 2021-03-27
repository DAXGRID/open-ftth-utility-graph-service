﻿using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanStructureRemoved
    {
        public Guid SpanEquipmentId { get; }
        public Guid SpanStructureId { get; }

        public SpanStructureRemoved(Guid spanEquipmentId, Guid spanStructureId)
        {
            SpanEquipmentId = spanEquipmentId;
            SpanStructureId = spanStructureId;
        }
    }
}
