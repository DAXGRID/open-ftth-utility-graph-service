﻿using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record AdditionalStructuresAddedToSpanEquipment
    {
        public Guid SpanEquipmentId { get; }
        public SpanStructure[] SpanStructuresToAdd {get; }

        public AdditionalStructuresAddedToSpanEquipment(Guid spanEquipmentId, SpanStructure[] spanStructuresToAdd)
        {
            SpanEquipmentId = spanEquipmentId;
            SpanStructuresToAdd = spanStructuresToAdd;
        }
    }
}
