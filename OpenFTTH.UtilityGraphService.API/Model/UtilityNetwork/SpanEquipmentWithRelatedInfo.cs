using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipmentWithRelatedInfo : SpanEquipment
    {
        public SpanSegmentRouteNetworkTraceRef[]? RouteNetworkTraceRefs { get; init; }

        public SpanEquipmentWithRelatedInfo(SpanEquipment original) : base(original) { }
    }
}
