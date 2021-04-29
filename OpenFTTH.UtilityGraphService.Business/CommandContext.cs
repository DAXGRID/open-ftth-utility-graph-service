using OpenFTTH.CQRS;
using System;

namespace OpenFTTH.UtilityGraphService.Business
{
    public record CommandContext
    {
        public Guid CmdId { get; }
        public UserContext UserContext { get; }

        public CommandContext(Guid cmdId, UserContext userContext)
        {
            CmdId = cmdId;
            UserContext = userContext;
        }
    }
}
