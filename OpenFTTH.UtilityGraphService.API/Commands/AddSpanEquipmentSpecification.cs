using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class AddSpanEquipmentSpecification : ICommand<Result>
    {
        public SpanEquipmentSpecification Specification { get; }

        public AddSpanEquipmentSpecification(SpanEquipmentSpecification specification)
        {
            Specification = specification;
        }
    }
}
