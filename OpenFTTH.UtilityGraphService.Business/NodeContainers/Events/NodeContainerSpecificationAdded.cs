using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record NodeContainerSpecificationAdded
    {
        public NodeContainerSpecification Specification { get; }

        public NodeContainerSpecificationAdded(NodeContainerSpecification specification)
        {
            Specification = specification;
        }
    }
}
