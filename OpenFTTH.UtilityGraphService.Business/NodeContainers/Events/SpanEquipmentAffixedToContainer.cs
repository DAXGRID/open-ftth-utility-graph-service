﻿using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Events
{
    public record SpanEquipmentAffixedToContainer
    {
        public Guid SpanEquipmentId { get; }
        public SpanEquipmentNodeContainerAffix Affix { get; }

        public SpanEquipmentAffixedToContainer(Guid spanEquipmentId, SpanEquipmentNodeContainerAffix affix)
        {
            SpanEquipmentId = spanEquipmentId;
            Affix = affix;
        }
    }
}
