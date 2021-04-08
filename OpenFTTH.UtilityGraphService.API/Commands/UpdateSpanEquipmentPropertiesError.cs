using FluentResults;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class UpdateSpanEquipmentPropertiesError : Error
    {
        public UpdateSpanEquipmentPropertiesErrorCodes Code { get; }
        public UpdateSpanEquipmentPropertiesError(UpdateSpanEquipmentPropertiesErrorCodes errorCode, string errorMsg) : base(errorCode.ToString() + ": " + errorMsg)
        {
            this.Code = errorCode;
            Metadata.Add("ErrorCode", errorCode.ToString());
        }
    }
}
