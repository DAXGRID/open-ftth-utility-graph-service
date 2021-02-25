using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record NodeTraceEnd
    {
        public Guid RouteNodeId { get; set; }
        public Guid TerminalId { get; set; }
    }
}
