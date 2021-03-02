﻿using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanEquipmentSpecificationAdded
    {
        public SpanEquipmentSpecification Specification { get; }

        public SpanEquipmentSpecificationAdded(SpanEquipmentSpecification specification)
        {
            Specification = specification;
        }
    }
}