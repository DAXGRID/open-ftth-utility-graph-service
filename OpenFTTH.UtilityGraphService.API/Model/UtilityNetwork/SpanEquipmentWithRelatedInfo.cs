using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanEquipmentWithRelatedInfo : SpanEquipment
    {
        public LookupCollection<SpanSegmentTrace>? Traces { get; init; }

        public SpanEquipmentWithRelatedInfo(SpanEquipment original) : base(original) { }
    }
}
