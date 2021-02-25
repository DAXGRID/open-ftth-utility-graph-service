using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing
{
    public record NodeTraceIntermediate
    {
        public Guid RouteNodeId { get; set; }
        public Guid ToTerminalId { get; set; }
        public Guid FromTerminalId { get; set; }
    }
}
