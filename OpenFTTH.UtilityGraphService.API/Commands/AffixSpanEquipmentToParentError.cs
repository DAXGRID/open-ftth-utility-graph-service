using FluentResults;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class AffixSpanEquipmentToParentError : Error
    {
        public AffixSpanEquipmentToParentErrorCodes Code { get; }
        public static AffixSpanEquipmentToParentErrorCodes INVALID_SPAN_SEGMENT_ID_NOT_FOUND { get; set; }
        public static AffixSpanEquipmentToParentErrorCodes NO_CABLE_SPAN_SEGMENT_NOT_FOUND { get; set; }
        public static AffixSpanEquipmentToParentErrorCodes NO_CONDUIT_SPAN_SEGMENT_NOT_FOUND { get; set; }

        public AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes errorCode, string errorMsg) : base(errorCode.ToString() + ": " + errorMsg)
        {
            this.Code = errorCode;
            Metadata.Add("ErrorCode", errorCode.ToString());
        }
    }
}
