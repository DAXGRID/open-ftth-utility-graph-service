using OpenFTTH.Events;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentAffixedToParent : EventStoreBaseEvent
    {
        public Guid SpanEquipmentId { get; }
        public SpanEquipmentSpanEquipmentAffix[] ParentAffixes { get; init; }

        public SpanEquipmentAffixedToParent(Guid spanEquipmentId, SpanEquipmentSpanEquipmentAffix[] parentAffixes)
        {
            SpanEquipmentId = spanEquipmentId;
            ParentAffixes = parentAffixes;
        }
    }
}
