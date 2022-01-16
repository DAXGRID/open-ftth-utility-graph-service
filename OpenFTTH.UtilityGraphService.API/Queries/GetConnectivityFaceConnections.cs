using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public class GetConnectivityFaceConnections : IQuery<Result<List<EquipmentConnectivityFaceConnectionInfo>>> 
    { 
        public Guid routeNodeId { get; }

        public Guid spanOrTerminalEquipmentId { get; }

        public ConnectivityDirectionEnum DirectionType { get; set; }

        public GetConnectivityFaceConnections(Guid routeNodeId, Guid spanOrTerminalEquipmentId, ConnectivityDirectionEnum directionType)
        {
            this.routeNodeId = routeNodeId;
            this.spanOrTerminalEquipmentId = spanOrTerminalEquipmentId;
            DirectionType = directionType;
        }
    }
}
