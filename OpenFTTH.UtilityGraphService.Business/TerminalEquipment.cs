using CSharpFunctionalExtensions;
using OpenFTTH.UtilityGraphService.EventSourcing;
using OpenFTTH.UtilityGraphService.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork.Events;
using OpenFTTH.UtilityGraphService.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Business
{
    public class TerminalEquipment : AggregateBase
    {
        public TerminalEquipment(
            IUtilityGraphQueries queryApi, 
            Guid routeNodeId, 
            Guid terminalEquipmentId, 
            Guid equipmentSpecificationId,
            Guid? equipmentProductAssetModelId = null,
            Guid? parentEquipmentId = null)
        {
            // Check that route node exists
            if (queryApi.GetRouteNode(routeNodeId).HasNoValue)
                throw new ArgumentException($"Route node with id: {routeNodeId} do not exists.");

            // Check that a node equipment with the specified id do not already exists
            if (queryApi.GetTerminalEquipment(terminalEquipmentId).HasValue)
                throw new ArgumentException($"A terminal equipment with id: {terminalEquipmentId} already exists.");

            RaiseEvent(new TerminalEquipmentPlaced(), false);
        }
    }
}
