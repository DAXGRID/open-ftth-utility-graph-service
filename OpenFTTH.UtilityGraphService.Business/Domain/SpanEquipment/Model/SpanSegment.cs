using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    public class SpanSegment
    {
        public int SequenceNumber { get; set; }
        public Guid FromTerminalEquipmentRef { get; set; }
        public Guid FromTerminalRef { get; set; }
        public Guid ToTerminalEquipmentRef { get; set; }
        public Guid ToTerminalRef { get; set; }

        /*
        public SpanEquipmentAggregateRoot? Parent { get; set; }
        public Int16 SpanStructureIndex { get; set; }
        public Int16 SpanSegmentIndex { get; set; }
        public Int16 FromRouteNodeIndex { get; set; }
        public Int16 ToRouteNodeIndex { get; set; }
        public  Terminal? FromTerminalNode { get; set; }
        public  Terminal? ToTerminalNode { get; set; }
        */
    }
}
