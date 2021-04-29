using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record RemoveSpanStructureFromSpanEquipment : BaseCommand, ICommand<Result>
    {
        public Guid SpanSegmentId { get; }

        public RemoveSpanStructureFromSpanEquipment(Guid spanSegmentId)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            SpanSegmentId = spanSegmentId;
        }
    }
}
