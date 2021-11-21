using OpenFTTH.Core;
using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record UtilityNetworkTrace : IIdentifiedObject
    {
        public Guid Id { get; }
        public Guid? FromTerminalId { get; }
        public Guid? ToTerminalId { get; }
        public Guid[] SpanSegmentIds { get; }

        public string? Name => null;
        public string? Description => null;

        public UtilityNetworkTrace(Guid spanSegmentId, Guid? fromTerminalId, Guid? toTerminalId, Guid[] spanSegmentIds)
        {
            Id = spanSegmentId;
            FromTerminalId = fromTerminalId;
            ToTerminalId = toTerminalId;
            SpanSegmentIds = spanSegmentIds;
        }
    }
}
