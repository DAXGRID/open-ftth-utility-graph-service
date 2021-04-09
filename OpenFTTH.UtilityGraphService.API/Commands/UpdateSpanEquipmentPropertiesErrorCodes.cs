﻿namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public enum UpdateSpanEquipmentPropertiesErrorCodes
    {
        NO_CHANGE_TO_MARKING_INFO,
        NO_CHANGE,
        NO_CHANGE_TO_MANUFACTURER,
        NO_CHANGE_TO_SPECIFICATION,
        CANNOT_CHANGE_FROM_NON_FIXED_TO_FIXED,
        CANNOT_REMOVE_SPAN_STRUCTURE_WITH_CONNECTED_SEGMENTS_FROM_SPAN_EQUIPMENT
    }
}
