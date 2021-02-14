using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class DeprecateSpanStructureSpecification : ICommand<Result>
    {
        public Guid SpanStructureSpecificationId { get; }

        public DeprecateSpanStructureSpecification(Guid spanStructureSpecificationId)
        {
            SpanStructureSpecificationId = spanStructureSpecificationId;
        }
    }
}
