using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record SpanSegment
    {
        public Guid Id { get; }
        public UInt16 FromNodeOfInterestIndex { get; }
        public UInt16 ToNodeOfInterestIndex { get; }
        public Guid FromTerminalId { get; set; }
        public Guid ToTerminalId { get; set; }

        public SpanSegment(Guid id, ushort fromNodeOfInterestIndex, ushort toNodeOfInterestIndex)
        {
            Id = id;
            FromNodeOfInterestIndex = fromNodeOfInterestIndex;
            ToNodeOfInterestIndex = toNodeOfInterestIndex;
        }
    }
}
