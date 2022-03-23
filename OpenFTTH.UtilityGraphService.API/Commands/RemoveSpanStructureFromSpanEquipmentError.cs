using FluentResults;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class RemoveSpanStructureFromSpanEquipmentError : Error
    {
        public RemoveSpanStructureFromSpanEquipmentErrorCodes Code { get; }
        public static RemoveSpanStructureFromSpanEquipmentErrorCodes SPAN_SEGMENT_CONTAIN_CABLE { get; set; }

        public RemoveSpanStructureFromSpanEquipmentError(RemoveSpanStructureFromSpanEquipmentErrorCodes errorCode, string errorMsg) : base(errorCode.ToString() + ": " + errorMsg)
        {
            this.Code = errorCode;
            Metadata.Add("ErrorCode", errorCode.ToString());
        }
    }
}
