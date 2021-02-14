using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events
{
    public record SpanStructureSpecificationAdded
    {
        public SpanStructureSpecification Specification { get; }

        public SpanStructureSpecificationAdded(SpanStructureSpecification specification)
        {
            Specification = specification;
        }
    }
}
