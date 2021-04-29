using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AddSpanEquipmentSpecification : BaseCommand, ICommand<Result>
    {
        public SpanEquipmentSpecification Specification { get; }

        public AddSpanEquipmentSpecification(SpanEquipmentSpecification specification)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            Specification = specification;
        }
    }
}
