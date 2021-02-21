using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
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
