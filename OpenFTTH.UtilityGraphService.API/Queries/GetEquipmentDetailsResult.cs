using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public record GetEquipmentDetailsResult
    {
        public LookupCollection<SpanEquipmentWithRelatedInfo>? SpanEquipment { get; init; }
        public LookupCollection<TerminalEquipment>? TerminalEquipment { get; init; }
        public LookupCollection<NodeContainer>? NodeContainers { get; init; }
        public LookupCollection<RouteNetworkTrace>? RouteNetworkTraces { get; init; }

        public GetEquipmentDetailsResult()
        {
        }
    }
}
