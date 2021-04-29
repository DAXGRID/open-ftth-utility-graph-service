using FluentResults;
using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record ReverseNodeContainerVerticalContentAlignment : BaseCommand, ICommand<Result>
    {
        public Guid NodeContainerId { get; }

        public ReverseNodeContainerVerticalContentAlignment(Guid nodeContainerId)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            NodeContainerId = nodeContainerId;
        }
    }
}
