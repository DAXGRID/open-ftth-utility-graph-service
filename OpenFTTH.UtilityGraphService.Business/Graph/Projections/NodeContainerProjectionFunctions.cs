using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;

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

    }
}
