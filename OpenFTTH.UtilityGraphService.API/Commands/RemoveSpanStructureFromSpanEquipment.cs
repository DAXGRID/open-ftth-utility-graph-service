using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record RemoveSpanStructureFromSpanEquipment : ICommand<Result>
    {
        public Guid SpanSegmentId { get; }

        public RemoveSpanStructureFromSpanEquipment(Guid spanSegmentId)
        {
            SpanSegmentId = spanSegmentId;
        }
    }
}
