using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record ConnectSpanEquipmentAndTerminalEquipment : BaseCommand, ICommand<Result>
    {
        public Guid RouteNodeId { get; }
        public ConnectSpanSegmentToTerminalOperation[] Connects { get; }

        public ConnectSpanEquipmentAndTerminalEquipment(Guid correlationId, UserContext userContext, Guid routeNodeId, ConnectSpanSegmentToTerminalOperation[] connects) : base(correlationId, userContext)
        {
            RouteNodeId = routeNodeId;
            Connects = connects;
        }
    }

    public record ConnectSpanSegmentToTerminalOperation
    {
        public Guid SpanSegmentId { get; }
        public Guid TerminalId { get; }

        public ConnectSpanSegmentToTerminalOperation(Guid spanSegmentId, Guid terminalId)
        {
            SpanSegmentId = spanSegmentId;
            TerminalId = terminalId;
        }
    }
}
