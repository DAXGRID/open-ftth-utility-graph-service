using DAX.ObjectVersioning.Graph;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraphConnectedSegment : GraphEdge, IUtilityGraphSegmentRef
    {
        private readonly SpanEquipment _spanEquipment;
        private readonly UInt16 _structureIndex;
        private readonly UInt16 _segmentIndex;

        public UtilityGraphConnectedSegment(Guid id, UtilityGraphConnectedTerminal fromNode, UtilityGraphConnectedTerminal toNode, SpanEquipment spanEquipment, UInt16 structureIndex, UInt16 segmentIndex) : base(id, fromNode, toNode)
        {
            _spanEquipment = spanEquipment;
            _structureIndex = structureIndex;
            _segmentIndex = segmentIndex;

            // Check that structure and span index is not out of bounds
            if (_structureIndex < 0 || _structureIndex >= _spanEquipment.SpanStructures.Length)
                throw new ArgumentException("Structure index out of bounds");

            SpanStructure spanStructure = _spanEquipment.SpanStructures[_structureIndex];

            if (_segmentIndex < 0 || _segmentIndex >= spanStructure.SpanSegments.Length)
                throw new ArgumentException("Segment index out of bounds");
        }

        public SpanEquipment SpanEquipment => _spanEquipment;

        public SpanSegment SpanSegment => _spanEquipment.SpanStructures[_structureIndex].SpanSegments[_segmentIndex];
    }
}
