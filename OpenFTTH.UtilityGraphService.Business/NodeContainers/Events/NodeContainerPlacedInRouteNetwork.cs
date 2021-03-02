using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record NodeContainerPlacedInRouteNetwork
    {
        public NodeContainer Container { get; }

        public NodeContainerPlacedInRouteNetwork(NodeContainer container)
        {
            this.Container = container;
        }
    }
}
