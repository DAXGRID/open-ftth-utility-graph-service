using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanSegmentDisconnectedFromTerminal
    {
        public Guid SpanEquipmentId { get; }
        public Guid SpanSegmentId { get; }
        public Guid TerminalId { get; }

        public SpanSegmentDisconnectedFromTerminal(Guid spanEquipmentId, Guid spanSegmentId, Guid terminalId)
        {
            SpanEquipmentId = spanEquipmentId;
            SpanSegmentId = spanSegmentId;
            TerminalId = terminalId;
        }
    }
}
