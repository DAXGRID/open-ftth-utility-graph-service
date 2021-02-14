using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanSegment
    {
        public int SequenceNumber { get; set; }
        public Guid FromTerminalId { get; set; }
        public Guid ToTerminalId { get; set; }
    }
}
