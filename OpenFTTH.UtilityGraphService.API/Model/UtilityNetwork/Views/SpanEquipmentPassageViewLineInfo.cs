using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    /// <summary>
    /// Represents a line for display in a span equipment connectivity view
    /// </summary>
    public record SpanEquipmentPassageViewLineInfo
    {
        public Guid SpanSegmentId { get; }
        public string? From { get; init; }
        public string? To { get; init; }
        public string? ConduitId { get; init; }
        public string? OuterConduitInfo { get; init; }
        public string? InnerConduitInfo { get; init; }

        public SpanEquipmentPassageViewLineInfo(Guid spanSegmentId)
        {
            SpanSegmentId = spanSegmentId;
        }
    }
}
