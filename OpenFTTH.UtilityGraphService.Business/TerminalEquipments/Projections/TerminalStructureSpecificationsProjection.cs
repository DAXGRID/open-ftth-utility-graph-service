using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Events;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections
{
    public class TerminalStructureSpecificationsProjection : ProjectionBase
    {
        private readonly LookupCollection<TerminalStructureSpecification> _terminalStructureSpecifications = new LookupCollection<TerminalStructureSpecification>();

        public LookupCollection<TerminalStructureSpecification> Specifications => _terminalStructureSpecifications;

        public TerminalStructureSpecificationsProjection()
        {
            ProjectEvent<TerminalStructureSpecificationAdded>(Project);
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data) 
            {
                case (TerminalStructureSpecificationAdded @event):
                    _terminalStructureSpecifications.Add(@event.Specification);
                    break;
            }
        }
    }
}
