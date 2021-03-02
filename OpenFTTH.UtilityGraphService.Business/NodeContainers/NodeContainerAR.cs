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
        private SpanEquipment? _spanEquipment;

        public NodeContainerAR()
        {
            Register<SpanEquipmentPlacedInRouteNetwork>(Apply);
        }

        public Result PlaceNodeConstainerInRouteNetworkNode(
            LookupCollection<SpanEquipment> nodeContainers,
            LookupCollection<SpanEquipmentSpecification> nodeContainerSpecifications,
            Guid nodeContainerId, 
            Guid nodeContainerSpecificationId,
            RouteNetworkInterest nodeOfInterest,
            Guid? manufacturerId
        )
        {
            this.Id = nodeContainerId;

            if (nodeContainerId == Guid.Empty)
                return Result.Fail(new PlaceNodeContainerInRouteNetworkError(PlaceNodeContainerInRouteNetworkErrorCodes.INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY, "Node container id cannot be empty. A unique id must be provided by client."));

            return Result.Ok();
        }
      
        private void Apply(SpanEquipmentPlacedInRouteNetwork obj)
        {
            _spanEquipment = obj.Equipment;
        }
    }
}
