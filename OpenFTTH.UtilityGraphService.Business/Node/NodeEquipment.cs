using CSharpFunctionalExtensions;
using OpenFTTH.UtilityGraphService.EventSourcing;
using OpenFTTH.UtilityGraphService.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork.Events;
using OpenFTTH.UtilityGraphService.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Business.Node
{
    public class NodeEquipment : AggregateBase
    {

        public NodeEquipment(
            IUtilityGraphQueries queryApi, 
            Guid routeNodeId, 
            Guid nodeEquipmentId, 
            Guid specificationId, 
            Guid? parentEquipmentContainerId = null)
        {

            // Check that route node exists
            if (queryApi.GetRouteNode(routeNodeId).HasNoValue)
                throw new ArgumentException($"Route node with id: {routeNodeId} do not exists.");

            // Check that a node equipment with the specified id do not already exists
            if (queryApi.GetNodeEquipment(nodeEquipmentId).HasValue)
                throw new ArgumentException($"A node equipment with id: {nodeEquipmentId} already exists.");

            RaiseEvent(new NodeEquipmentPlaced(), false);
        }
    }
}
