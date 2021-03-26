using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record SegmentTrace
    {
        public Guid NodeOfInterestId { get; set; }

        public SegmentTrace(Guid nodeOfInterestId)
        {
            NodeOfInterestId = nodeOfInterestId;
        }
    }
}
