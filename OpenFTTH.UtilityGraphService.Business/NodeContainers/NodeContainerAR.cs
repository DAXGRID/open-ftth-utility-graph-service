using FluentResults;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments
{
    /// <summary>
    /// The root structure placed in a route network node - i.e. cabinet, building, well, conduit closure etc.
    /// </summary>
    public class NodeContainerAR : AggregateBase
    {
        private NodeContainer? _container;

        public NodeContainerAR()
        {
            Register<NodeContainerPlacedInRouteNetwork>(Apply);
        }

        public Result PlaceNodeContainerInRouteNetworkNode(
            LookupCollection<NodeContainer> nodeContainers,
            LookupCollection<NodeContainerSpecification> nodeContainerSpecifications,
            Guid nodeContainerId, 
            Guid nodeContainerSpecificationId,
            RouteNetworkInterest nodeOfInterest,
            Guid? manufacturerId
        )
        {
            this.Id = nodeContainerId;

            if (nodeContainerId == Guid.Empty)
                return Result.Fail(new PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes.INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY, "Node container id cannot be empty. A unique id must be provided by client."));

            if (nodeContainers.ContainsKey(nodeContainerId))
                return Result.Fail(new PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes.INVALID_NODE_CONTAINER_ID_ALREADY_EXISTS, $"A node container with id: {nodeContainerId} already exists."));

            if (nodeOfInterest.Kind != RouteNetworkInterestKindEnum.NodeOfInterest)
                return Result.Fail(new PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes.INVALID_INTEREST_KIND_MUST_BE_NODE_OF_INTEREST, "Interest kind must be NodeOfInterest. You can only put node container into route nodes!"));

            if (!nodeContainerSpecifications.ContainsKey(nodeContainerSpecificationId))
                return Result.Fail(new PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes.INVALID_NODE_CONTAINER_SPECIFICATION_ID_NOT_FOUND, $"Cannot find node container specification with id: {nodeContainerSpecificationId}"));

            if (nodeContainers.Any(n => n.RouteNodeId == nodeOfInterest.RouteNetworkElementRefs[0]))
                return Result.Fail(new PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes.NODE_CONTAINER_ALREADY_EXISTS_IN_ROUTE_NODE, $"A node container already exist in the route node with id: {nodeOfInterest.RouteNetworkElementRefs[0]} Only one node container is allowed per route node.")); 

            var nodeContainer = new NodeContainer(nodeContainerId, nodeContainerSpecificationId, nodeOfInterest.Id, nodeOfInterest.RouteNetworkElementRefs[0])
            {
                ManufacturerId = manufacturerId
            };

            var nodeContainerPlaceInRouteNetworkEvent = new NodeContainerPlacedInRouteNetwork(nodeContainer);

            RaiseEvent(nodeContainerPlaceInRouteNetworkEvent);

            return Result.Ok();
        }
      
        private void Apply(NodeContainerPlacedInRouteNetwork obj)
        {
            _container = obj.Container;
        }
    }
}
