using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;

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

    }
}
