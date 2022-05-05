using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Events;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Projections
{
    /// <summary>
    /// Functions that apply events to a span equipment immutable object and return a new copy
    /// </summary>
    public static class TerminalEquipmentProjectionFunctions
    {
        public static TerminalEquipment Apply(TerminalEquipment existingSpanEquipment, TerminalEquipmentNamingInfoChanged @event)
        {
            return existingSpanEquipment with
            {
                NamingInfo = @event.NamingInfo
            };
        }

        public static TerminalEquipment Apply(TerminalEquipment existingSpanEquipment, TerminalEquipmentAddressInfoChanged @event)
        {
            return existingSpanEquipment with
            {
                AddressInfo = @event.AddressInfo
            };
        }

        public static TerminalEquipment Apply(TerminalEquipment existingSpanEquipment, TerminalEquipmentManufacturerChanged @event)
        {
            return existingSpanEquipment with
            {
                ManufacturerId = @event.ManufacturerId
            };
        }

        public static TerminalEquipment Apply(TerminalEquipment existingSpanEquipment, TerminalEquipmentSpecificationChanged @event)
        {
            return existingSpanEquipment with
            {
                SpecificationId = @event.NewSpecificationId
            };
        }

        public static TerminalEquipment Apply(TerminalEquipment existingSpanEquipment, AdditionalStructuresAddedToTerminalEquipment @event)
        {
            List<TerminalStructure> newTerminalStructureList = new();

            if (existingSpanEquipment.TerminalStructures != null)
                newTerminalStructureList.AddRange(existingSpanEquipment.TerminalStructures);

            newTerminalStructureList.AddRange(@event.TerminalStructuresToAdd);

            return existingSpanEquipment with
            {
                TerminalStructures = newTerminalStructureList.ToArray()
            };
        }

    }
}
