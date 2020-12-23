using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.Model.Specification;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Domain.SpanEquipmentSpecification
{
    public class SpanEquipmentSpecificationAggregate : AggregateBase
    {
        public SpanEquipmentSpecificationAggregate(
            Guid id,
            string name,
            string version,
            SpanStructureSpecification structure)
        {

        }
    }
}
