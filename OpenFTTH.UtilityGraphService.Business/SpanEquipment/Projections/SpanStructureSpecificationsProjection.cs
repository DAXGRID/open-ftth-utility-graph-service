﻿using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Util;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections
{
    public class SpanStructureSpecificationsProjection : ProjectionBase
    {
        private readonly LookupCollection<SpanStructureSpecification> _spanStructureSpecifications = new LookupCollection<SpanStructureSpecification>();

        public LookupCollection<SpanStructureSpecification> Specifications => _spanStructureSpecifications;

        public SpanStructureSpecificationsProjection()
        {
            ProjectEvent<SpanStructureSpecificationAdded>(Project);
            ProjectEvent<SpanStructureSpecificationDeprecated>(Project);
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanStructureSpecificationAdded @event):
                    _spanStructureSpecifications.Add(@event.Specification);
                    break;

                case (SpanStructureSpecificationDeprecated @event):
                    _spanStructureSpecifications[@event.SpanStructureSpecificationId] = _spanStructureSpecifications[@event.SpanStructureSpecificationId] with { Deprecated = true };
                    break;
            }
        }
    }
}
