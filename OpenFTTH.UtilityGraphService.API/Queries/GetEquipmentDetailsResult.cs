using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public record GetEquipmentDetailsResult
    {
        public LookupCollection<SpanEquipment> SpanEquipment { get; }

        public GetEquipmentDetailsResult(SpanEquipment[] spanEquipment)
        {
            SpanEquipment = new LookupCollection<SpanEquipment>(spanEquipment);
        }
    }
}
