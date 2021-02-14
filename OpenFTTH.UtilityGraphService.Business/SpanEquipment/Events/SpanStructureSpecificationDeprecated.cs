using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events
{
    public record SpanStructureSpecificationDeprecated
    {
        public Guid SpanStructureSpecificationId { get; }

        public SpanStructureSpecificationDeprecated(Guid spanStructureSpecificationId)
        {
            SpanStructureSpecificationId = spanStructureSpecificationId;
        }
    }
}
