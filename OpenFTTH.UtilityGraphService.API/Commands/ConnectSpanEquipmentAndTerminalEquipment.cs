using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record ConnectSpanEquipmentAndTerminalEquipment : BaseCommand, ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public Guid SpanEquipmentId { get; }
        public Guid[] SpanSegmentsIds { get; }
        public Guid TerminalEquipmentId { get; }
        public Guid[] TerminalIds { get; }

        public ConnectSpanEquipmentAndTerminalEquipment(Guid correlationId, UserContext userContext, Guid routeNodeId, Guid spanEquipmentId, Guid[] spanSegmentsIds, Guid terminalEquipmentId, Guid[] terminalIds) : base(correlationId, userContext)
        {
            RouteNodeId = routeNodeId;
            SpanEquipmentId = spanEquipmentId;
            SpanSegmentsIds = spanSegmentsIds;
            TerminalEquipmentId = terminalEquipmentId;
            TerminalIds = terminalIds;
        }
    }
}
