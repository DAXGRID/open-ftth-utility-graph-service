using OpenFTTH.Events;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentAffixedToParent : EventStoreBaseEvent
    {
        public Guid SpanEquipmentId { get; }
        public UtilityNetworkHop NewUtilityHop { get; init; }

        public SpanEquipmentAffixedToParent(Guid spanEquipmentId, UtilityNetworkHop utilityHop)
        {
            SpanEquipmentId = spanEquipmentId;
            NewUtilityHop = utilityHop;
        }
    }
}
