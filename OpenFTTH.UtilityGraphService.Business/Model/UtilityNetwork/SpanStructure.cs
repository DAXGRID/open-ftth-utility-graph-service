using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    public struct SpanStructure
    {
        public Int16 Level { get; set; }
        public Int16 ParentPosition { get; set; }
        public SpanSegment[] SpanSegments { get; set; }
    }
}
