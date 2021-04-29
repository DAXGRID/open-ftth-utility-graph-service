using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record DeprecateSpanStructureSpecification : BaseCommand, ICommand<Result>
    {
        public Guid SpanStructureSpecificationId { get; }

        public DeprecateSpanStructureSpecification(Guid spanStructureSpecificationId)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            SpanStructureSpecificationId = spanStructureSpecificationId;
        }
    }
}
