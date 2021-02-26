using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanSegment
    {
        public Guid Id { get; }
        public int SequenceNumber { get; set; }
        public Guid FromTerminalId { get; set; }
        public Guid ToTerminalId { get; set; }

        public SpanSegment(Guid id, int sequenceNumber)
        {
            this.Id = id;
            this.SequenceNumber = sequenceNumber;
        }
    }
}
