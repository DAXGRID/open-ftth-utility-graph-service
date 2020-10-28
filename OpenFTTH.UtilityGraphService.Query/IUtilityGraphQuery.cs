using CSharpFunctionalExtensions;
using OpenFTTH.UtilityGraphService.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Query
{
    public interface IUtilityGraphQueries
    {
        Maybe<IRouteNode> GetRouteNode(Guid routeNodeId);

        Maybe<ITerminalEquipment> GetTerminalEquipment(Guid terminalEquipmentId);
    }
}
