using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record UpdateSpanEquipmentProperties : BaseCommand, ICommand<Result>
    {
        public Guid SpanEquipmentOrSegmentId { get; }

        public Guid? SpecificationId { get; init; }
        public Guid? ManufacturerId { get; init; }
        public NamingInfo? NamingInfo { get; init; }
        public MarkingInfo? MarkingInfo { get; init; }

        public UpdateSpanEquipmentProperties(Guid spanEquipmentOrSegmentId)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            SpanEquipmentOrSegmentId = spanEquipmentOrSegmentId;
        }
    }
}
