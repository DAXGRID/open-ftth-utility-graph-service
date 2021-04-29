using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record PlaceAdditionalStructuresInSpanEquipment : BaseCommand, ICommand<Result>
    {
        public Guid SpanEquipmentId { get; }

        public Guid[] StructureSpecificationIds { get;  }

        public PlaceAdditionalStructuresInSpanEquipment(Guid spanEquipmentId, Guid[] structureSpecificationIds)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            SpanEquipmentId = spanEquipmentId;
            StructureSpecificationIds = structureSpecificationIds;
        }
    }
}
