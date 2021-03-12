using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanSegmentsConnectedToSimpleTerminals
    {
        public Guid SpanEquipmentId { get; }
        public SpanSegmentToSimpleTerminalConnectInfo[] Connects { get; }

        public SpanSegmentsConnectedToSimpleTerminals(Guid spanEquipmentId, SpanSegmentToSimpleTerminalConnectInfo[] connects)
        {
            SpanEquipmentId = spanEquipmentId;
            Connects = connects;
        }
    }

    public record SpanSegmentToSimpleTerminalConnectInfo
    {
        public Guid SegmentId { get; }
        public Guid TerminalId { get; set; }
        public SpanSegmentToTerminalConnectionDirection ConnectionDirection { get; set; }
        public UInt16 StructureIndex { get; }
        public UInt16 SegmentIndex { get; }

        public SpanSegmentToSimpleTerminalConnectInfo(Guid segmentId, Guid terminalId, ushort structureIndex, ushort segmentIndex)
        {
            SegmentId = segmentId;
            TerminalId = terminalId;
            StructureIndex = structureIndex;
            SegmentIndex = segmentIndex;
        }
    }

    public enum SpanSegmentToTerminalConnectionDirection
    {
        FromSpanSegmentToTerminal,
        FromTerminalToSpanSegment
    }
}
