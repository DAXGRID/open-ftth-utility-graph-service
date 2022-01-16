using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record UtilityNetworkHop
    {
        public Guid FromNodeId { get; }
        public Guid ToNodeId { get; }
        public SpanEquipmentSpanEquipmentAffix[] ParentAffixes { get; }

        public UtilityNetworkHop(Guid fromNodeId, Guid toNodeId, SpanEquipmentSpanEquipmentAffix[] parentAffixes)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            ParentAffixes = parentAffixes;
        }
    }
}
