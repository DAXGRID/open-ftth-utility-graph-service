﻿namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public enum MoveSpanEquipmentErrorCodes
    {
        INVALID_SPAN_EQUIPMENT_ID_CANNOT_BE_EMPTY,
        SPAN_EQUIPMENT_NOT_FOUND,
        NEW_WALK_EQUALS_EXISTING_WALK,
        CANNOT_MOVE_BOTH_ENDS_AT_THE_SAME_TIME_IF_SPAN_SEGMENT_HAS_CUTS,
        CANNOT_MOVE_FROM_END_BECAUSE_SEGMENTS_ARE_CONNECTED_THERE,
        CANNOT_MOVE_TO_END_BECAUSE_SEGMENTS_ARE_CONNECTED_THERE,
        CANNOT_MOVE_NODE_BECAUSE_SEGMENTS_ARE_CUT_THERE,
        CANNOT_MOVE_NODE_BECAUSE_SPAN_EQUIPMENT_IS_AFFIXED_TO_CONTAINER,
        CANNOT_MOVE_FROM_END_TO_NODE_WHERE_SEGMENTS_ARE_CUT,
        CANNOT_MOVE_TO_END_TO_NODE_WHERE_SEGMENTS_ARE_CUT,
        SPAN_SEGMENT_CONTAIN_CABLE,
        SPAN_EQUIPMENT_IS_AFFIXED_TO_CONDUIT,
        ERROR_MOVING_CHILD_SPAN_EQUIPMENT,
        ENDS_CANNOT_BE_MOVED_BECAUSE_OF_CHILD_SPAN_EQUIPMENTS,
        CANNOT_MOVE_SEGMENTS_AFFIXED_TO_PARENTS
    }
}
