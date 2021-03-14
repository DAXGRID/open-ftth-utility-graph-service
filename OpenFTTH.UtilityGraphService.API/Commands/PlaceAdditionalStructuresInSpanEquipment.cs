using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record PlaceAdditionalStructuresInSpanEquipment : ICommand<Result>
    {
        public Guid SpanEquipmentId { get; }

        public Guid[] StructureSpecificationIds { get;  }

        public PlaceAdditionalStructuresInSpanEquipment(Guid spanEquipmentId, Guid[] structureSpecificationIds)
        {
            SpanEquipmentId = spanEquipmentId;
            StructureSpecificationIds = structureSpecificationIds;
        }
    }
}
