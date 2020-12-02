using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    public class SpanSegment
    {
        public SpanEquipment? Parent { get; set; }
        public Int16 SpanStructureIndex { get; set; }
        public Int16 SpanSegmentIndex { get; set; }
        public Int16 FromRouteNodeIndex { get; set; }
        public Int16 ToRouteNodeIndex { get; set; }
        public  Terminal? FromTerminalNode { get; set; }
        public  Terminal? ToTerminalNode { get; set; }
    }
}
