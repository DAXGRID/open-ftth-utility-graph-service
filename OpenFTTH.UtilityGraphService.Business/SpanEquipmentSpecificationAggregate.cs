using OpenFTTH.UtilityGraphService.EventSourcing;
using OpenFTTH.UtilityGraphService.Model.Specification;
using OpenFTTH.UtilityGraphService.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Business
{
    public class SpanEquipmentSpecificationAggregate : AggregateBase
    {
        public SpanEquipmentSpecificationAggregate(
            IUtilityGraphQueries queryApi, 
            Guid id,
            string name,
            string version,
            SpanStructureSpecification structure)
        {

        }
    }
}
