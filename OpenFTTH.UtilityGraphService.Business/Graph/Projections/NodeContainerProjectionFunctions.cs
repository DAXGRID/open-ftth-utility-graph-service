using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Projections
{
    public static class NodeContainerProjectionFunctions
    {
        public static NodeContainer Apply(NodeContainer existingSpanEquipment, NodeContainerVerticalAlignmentReversed alignmentReversed)
        {
            var newAllignment = existingSpanEquipment.VertialContentAlignmemt == NodeContainerVerticalContentAlignmentEnum.Bottom ? NodeContainerVerticalContentAlignmentEnum.Top : NodeContainerVerticalContentAlignmentEnum.Bottom;

            return existingSpanEquipment with
            {
                VertialContentAlignmemt = newAllignment
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerManufacturerChanged @event)
        {
            return existingEquipment with
            {
                ManufacturerId = @event.ManufacturerId
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerSpecificationChanged @event)
        {
            return existingEquipment with
            {
                SpecificationId = @event.NewSpecificationId,
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, RackAddedToNodeContainer @event)
        {
            List<Rack> newRackList = new();

            if (existingEquipment.Racks != null)
                newRackList.AddRange(existingEquipment.Racks);

            newRackList.Add(new Rack(@event.RackId, @event.RackName, @event.RackPosition, @event.RackSpecificationId, @event.RackHeightInUnits, new SubrackMount[] { }));

            return existingEquipment with
            {
                Racks = newRackList.ToArray()
            };
        }
    }
}
