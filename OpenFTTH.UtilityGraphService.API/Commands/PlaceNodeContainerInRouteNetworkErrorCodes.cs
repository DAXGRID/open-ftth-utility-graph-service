﻿namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public enum PlaceNodeContainerInRouteNetworkErrorCodes
    {
        INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY,
        INVALID_NODE_CONTAINER_ID_ALREADY_EXISTS,
        INVALID_INTEREST_KIND_MUST_BE_NODE_OF_INTEREST,
        INVALID_NODE_CONTAINER_SPECIFICATION_ID_NOT_FOUND,
        NODE_CONTAINER_ALREADY_EXISTS_IN_ROUTE_NODE
    }
}
