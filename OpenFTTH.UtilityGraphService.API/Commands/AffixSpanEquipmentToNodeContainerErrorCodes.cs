﻿namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public enum AffixSpanEquipmentToNodeContainerErrorCodes
    {
        INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY,
        INVALID_SPAN_SEGMENT_ID_CANNOT_BE_EMPTY,
        INVALID_SPAN_CONTAINER_ID_NOT_FOUND,
        INVALID_SPAN_SEGMENT_ID_NOT_FOUND,
        SPAN_EQUIPMENT_AND_NODE_CONTAINER_IS_NOT_COLOCATED,
        SPAN_EQUIPMENT_ALREADY_AFFIXED_TO_NODE_CONTAINER,
    }
}
