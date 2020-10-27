using CSharpFunctionalExtensions;
using OpenFTTH.UtilityGraphService.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Model.RouteNetwork;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenFTTH.UtilityGraphService.Query.RouteNetworkEventHandling;
using OpenFTTH.Events.RouteNetwork;

namespace OpenFTTH.UtilityGraphService.Query.InMemory
{
    public class InMemoryQueryHandler : IUtilityGraphQueries
    {
        private ILoggerFactory _loggerFactory;
        private readonly ILogger<InMemoryQueryHandler> _logger;

        private InMemoryNetworkState _state = new InMemoryNetworkState();

        public InMemoryQueryHandler(ILoggerFactory loggerFactory)
        {
            if (null == loggerFactory)
            {
                throw new ArgumentNullException("loggerFactory cannot be null");
            }

            _loggerFactory = loggerFactory;

            _logger = loggerFactory.CreateLogger<InMemoryQueryHandler>();
        }


        /// <summary>
        /// Use this method to seed the in memory state with route network data
        /// </summary>
        public void Seed(List<RouteNetworkEditOperationOccuredEvent> editOperationEvents)
        {
            var routeNetworkEventHandler = new RouteNetworkEventHandler(_loggerFactory, _state);

            foreach (var editOperationEvent in editOperationEvents)
                routeNetworkEventHandler.HandleEvent(editOperationEvent);
        }

        public Maybe<INodeEquipment> GetNodeEquipment(Guid nodeEquipmentId)
        {
            var obj = _state.GetObject(nodeEquipmentId);

            if (obj != null && obj is INodeEquipment)
                return Maybe<INodeEquipment>.From((INodeEquipment)obj);

            return Maybe<INodeEquipment>.None;
        }

        public Maybe<IRouteNode> GetRouteNode(Guid routeNodeId)
        {
            var obj = _state.GetObject(routeNodeId);

            if (obj != null && obj is IRouteNode)
                return Maybe<IRouteNode>.From((IRouteNode)obj);

            return Maybe<IRouteNode>.None;
        }
    }
}
