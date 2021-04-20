using FluentResults;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class ReverseNodeContainerVerticalContentAlignmentError : Error
    {
        public ReverseNodeContainerVerticalContentAlignmentErrorCodes Code { get; }
        public ReverseNodeContainerVerticalContentAlignmentError(ReverseNodeContainerVerticalContentAlignmentErrorCodes errorCode, string errorMsg) : base(errorCode.ToString() + ": " + errorMsg)
        {
            this.Code = errorCode;
            Metadata.Add("ErrorCode", errorCode.ToString());
        }
    }
}
