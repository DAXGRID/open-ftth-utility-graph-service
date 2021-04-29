using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AddNodeContainerSpecification : BaseCommand, ICommand<Result>
    {
        public NodeContainerSpecification Specification { get; }

        public AddNodeContainerSpecification(NodeContainerSpecification specification)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            Specification = specification;
        }
    }
}
