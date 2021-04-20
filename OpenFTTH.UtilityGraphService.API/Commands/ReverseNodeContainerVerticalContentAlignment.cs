using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record ReverseNodeContainerVerticalContentAlignment : ICommand<Result>
    {
        public Guid NodeContainerId { get; }

        public ReverseNodeContainerVerticalContentAlignment(Guid nodeContainerId)
        {
            NodeContainerId = nodeContainerId;
        }
    }
}
