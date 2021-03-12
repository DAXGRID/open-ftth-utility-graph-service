using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    /// <summary>
    /// Used when a span segment is unused - i.e. an inner conduit or fiber is yet to be used/connected.
    /// This to prevent using tons of memory (arround 200+ bytes due to GraphEdge holding two dicts).
    /// So we use this light weight class (that is not derived from GraphEdge) to represent the not 
    /// connected span segments.
    /// </summary>
    public class UtilityGraphDisconnectedSegment : IUtilityGraphSegmentRef
    {
        private readonly SpanEquipment _spanEquipment;
        private readonly UInt16 _structureIndex;
        private readonly UInt16 _segmentIndex;

        public UtilityGraphDisconnectedSegment(SpanEquipment spanEquipment, UInt16 structureIndex, UInt16 segmentIndex)
        {
            _spanEquipment = spanEquipment;
            _structureIndex = structureIndex;
            _segmentIndex = segmentIndex;

            // Check that structure index is not out of bounds
            if (_structureIndex < 0 || _structureIndex >= _spanEquipment.SpanStructures.Length)
                throw new ArgumentException("Structure index out of bounds");

            // Check that span index is not out of bounds
            if (_segmentIndex < 0 || _segmentIndex >= _spanEquipment.SpanStructures[_structureIndex].SpanSegments.Length)
                throw new ArgumentException("Segment index out of bounds");
        }

        public SpanEquipment SpanEquipment => _spanEquipment;

        public SpanSegment SpanSegment => _spanEquipment.SpanStructures[_structureIndex].SpanSegments[_segmentIndex];
    }
}
