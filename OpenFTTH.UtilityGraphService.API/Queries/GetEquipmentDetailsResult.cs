using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public record GetEquipmentDetailsResult
    {
        public LookupCollection<SpanEquipmentWithRelatedInfo> SpanEquipment { get; }

        public GetEquipmentDetailsResult(SpanEquipmentWithRelatedInfo[] spanEquipment)
        {
            SpanEquipment = new LookupCollection<SpanEquipmentWithRelatedInfo>(spanEquipment);
        }
    }
}
