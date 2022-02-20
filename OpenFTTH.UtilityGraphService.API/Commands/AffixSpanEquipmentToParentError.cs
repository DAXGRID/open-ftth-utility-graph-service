using FluentResults;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class AffixSpanEquipmentToParentError : Error
    {
        public AffixSpanEquipmentToParentErrorCodes Code { get; }
        public static AffixSpanEquipmentToParentErrorCodes INVALID_SPAN_SEGMENT_ID_NOT_FOUND { get; set; }
        public static AffixSpanEquipmentToParentErrorCodes NO_CABLE_SPAN_SEGMENT_NOT_FOUND { get; set; }
        public static AffixSpanEquipmentToParentErrorCodes NO_CONDUIT_SPAN_SEGMENT_NOT_FOUND { get; set; }
        public static AffixSpanEquipmentToParentErrorCodes NON_MULTI_LEVEL_CONDUIT_CANNOT_CONTAIN_MORE_THAN_ONE_CABLE { get; set; }
        public static AffixSpanEquipmentToParentErrorCodes CONDUIT_SEGMENT_ALREADY_CONTAIN_CABLE { get; set; }

        public AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes errorCode, string errorMsg) : base(errorCode.ToString() + ": " + errorMsg)
        {
            this.Code = errorCode;
            Metadata.Add("ErrorCode", errorCode.ToString());
        }
    }
}
