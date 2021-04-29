using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class AddSpanStructureSpecification : ICommand<Result>
    {
        public SpanStructureSpecification Specification { get; }

        public AddSpanStructureSpecification(SpanStructureSpecification specification)
        {
            Specification = specification;
        }
    }
}
