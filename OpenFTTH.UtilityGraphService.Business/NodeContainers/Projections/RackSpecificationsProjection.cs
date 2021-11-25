using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers.Projections
{
    public class RackSpecificationsProjection : ProjectionBase
    {
        private readonly LookupCollection<RackSpecification> _rackSpecifications = new LookupCollection<RackSpecification>();

        public LookupCollection<RackSpecification> Specifications => _rackSpecifications;

        public RackSpecificationsProjection()
        {
            ProjectEvent<RackSpecificationAdded>(Project);
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (RackSpecificationAdded @event):
                    _rackSpecifications.Add(@event.Specification);
                    break;
            }
        }
    }
}
