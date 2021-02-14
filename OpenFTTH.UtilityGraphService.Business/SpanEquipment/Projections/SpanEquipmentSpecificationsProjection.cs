using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Util;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections
{
    public class SpanEquipmentSpecificationsProjection : ProjectionBase
    {
        private readonly LookupCollection<SpanEquipmentSpecification> _spanEquipmentSpecifications = new LookupCollection<SpanEquipmentSpecification>();

        public LookupCollection<SpanEquipmentSpecification> Specifications => _spanEquipmentSpecifications;

        public SpanEquipmentSpecificationsProjection()
        {
            ProjectEvent<SpanEquipmentSpecificationAdded>(Project);
            ProjectEvent<SpanEquipmentSpecificationDeprecated>(Project);
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanEquipmentSpecificationAdded @event):
                    _spanEquipmentSpecifications.Add(@event.Specification);
                    break;

                case (SpanEquipmentSpecificationDeprecated @event):
                    _spanEquipmentSpecifications[@event.SpanEquipmentSpecificationId] = _spanEquipmentSpecifications[@event.SpanEquipmentSpecificationId] with { Deprecated = true };
                    break;
            }
        }
    }
}
