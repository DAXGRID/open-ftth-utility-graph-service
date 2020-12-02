using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using OpenFTTH.UtilityGraphService.Business.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.QueryModel;
using System;

namespace OpenFTTH.UtilityGraphService.Query.InMemory
{
    public class InMemoryQueryHandler : IUtilityGraphQueries
    {
        private ILoggerFactory _loggerFactory;
        private readonly INetworkState _networkState;
        private readonly ILogger<InMemoryQueryHandler> _logger;

        public InMemoryQueryHandler(ILoggerFactory loggerFactory, INetworkState networkState)
        {
            if (null == loggerFactory)
            {
                throw new ArgumentNullException("loggerFactory cannot be null");
            }

            _loggerFactory = loggerFactory;

            _logger = loggerFactory.CreateLogger<InMemoryQueryHandler>();

            _networkState = networkState;
        }

        public Maybe<ITerminalEquipment> GetTerminalEquipment(Guid terminalEquipmentId)
        {
            return GetObject<ITerminalEquipment>(terminalEquipmentId);
        }

        public Maybe<IRouteNode> GetRouteNode(Guid routeNodeId)
        {
            return GetObject<IRouteNode>(routeNodeId);
        }

        public Maybe<Type> GetObject<Type>(Guid objectId)
        {
            var obj = _networkState.GetObject(objectId);

            if (obj != null && obj is Type)
                return Maybe<Type>.From((Type)obj);

            return Maybe<Type>.None;
        }

        public Maybe<RouteNetworkQueryResult> RouteNetworkQuery(RouteNetworkQueryRequest routeNetworkQueryRequest)
        {
            throw new NotImplementedException();
        }
    }
}
