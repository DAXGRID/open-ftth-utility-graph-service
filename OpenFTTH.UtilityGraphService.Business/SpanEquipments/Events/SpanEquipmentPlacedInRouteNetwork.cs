using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentPlacedInRouteNetwork
    {
        public SpanEquipment Equipment { get; }

        public SpanEquipmentPlacedInRouteNetwork(SpanEquipment equipment)
        {
            this.Equipment = equipment;
        }
    }
}
