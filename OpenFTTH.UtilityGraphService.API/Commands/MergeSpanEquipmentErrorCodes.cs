﻿namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public enum MergeSpanEquipmentErrorCodes
    {
        CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_OF_SPECIFICATION_MISMATCH,
        CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_OF_CONNECTIVITY,
        CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_ENDS_ARE_NOT_COLOCATED_IN_ROUTE_NODE,
        CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_END_IS_AFFIXED_TO_NODE_CONTAINER
    }
}