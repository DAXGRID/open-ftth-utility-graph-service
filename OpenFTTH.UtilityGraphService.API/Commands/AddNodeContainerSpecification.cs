using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class AddNodeContainerSpecification : ICommand<Result>
    {
        public NodeContainerSpecification Specification { get; }

        public AddNodeContainerSpecification(NodeContainerSpecification specification)
        {
            Specification = specification;
        }
    }
}
