using FluentResults;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class PlaceNodeContainerInRouteNetworkError : Error
    {
        public PlaceNodeContainerInRouteNetworkErrorCodes Code { get; }
        public PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes errorCode, string errorMsg) : base(errorCode.ToString() + ": " + errorMsg)
        {
            this.Code = errorCode;
            Metadata.Add("ErrorCode", errorCode.ToString());
        }
    }
}
