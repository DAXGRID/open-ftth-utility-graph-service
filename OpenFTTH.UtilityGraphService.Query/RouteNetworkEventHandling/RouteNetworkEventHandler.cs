using DAX.ObjectVersioning.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenFTTH.Events.RouteNetwork;
using OpenFTTH.UtilityGraphService.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.Query.InMemory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Query.RouteNetworkEventHandling
{
    public class RouteNetworkEventHandler
    {
        private readonly ILogger<RouteNetworkEventHandler> _logger;

        private INetworkState _networkState;

        private HashSet<Guid> _alreadyProcessed = new HashSet<Guid>();

        public RouteNetworkEventHandler(ILoggerFactory loggerFactory, INetworkState networkState)
        {
            if (null == loggerFactory)
            {
                throw new ArgumentNullException("loggerFactory is null");
            }

            _logger = loggerFactory.CreateLogger<RouteNetworkEventHandler>();

            _networkState = networkState;
        }

        public void HandleEvent(RouteNetworkEditOperationOccuredEvent request)
        {
            _logger.LogDebug("Got route network edit opreation occured message:");
            _logger.LogDebug(JsonConvert.SerializeObject(request, Formatting.Indented));

            var trans = _networkState.GetTransaction();

            if (request.RouteNetworkCommands != null)
            {
                foreach (var command in request.RouteNetworkCommands)
                {
                    if (command.RouteNetworkEvents != null)
                    {
                        foreach (var routeNetworkEvent in command.RouteNetworkEvents)
                        {
                            switch (routeNetworkEvent)
                            {
                                case RouteNodeAdded domainEvent:
                                    HandleEvent(domainEvent, trans);
                                    break;

                                case RouteNodeMarkedForDeletion domainEvent:
                                    HandleEvent(domainEvent, trans);
                                    break;

                                case RouteSegmentAdded domainEvent:
                                    HandleEvent(domainEvent, trans);
                                    break;

                                case RouteSegmentMarkedForDeletion domainEvent:
                                    HandleEvent(domainEvent, trans);
                                    break;

                                case RouteSegmentRemoved domainEvent:
                                    HandleEvent(domainEvent, trans);
                                    break;
                            }
                        }
                    }
                }
            }

            _networkState.FinishWithTransaction();
        }


        private void HandleEvent(RouteNodeAdded request, ITransaction transaction)
        {
            _logger.LogDebug($"Handler got {request.GetType().Name} event seq no: {request.EventSequenceNumber}");

            if (AlreadyProcessed(request.EventId))
                return;

            var envelope = GeoJsonConversionHelper.ConvertFromPointGeoJson(request.Geometry).Envelope.EnvelopeInternal;

            transaction.Add(new RouteNode(request.NodeId, request.RouteNodeInfo?.Function, envelope, request.NamingInfo?.Name), ignoreDublicates: true);
        }

        private void HandleEvent(RouteSegmentAdded request, ITransaction transaction)
        {
            _logger.LogDebug($"Handler got {request.GetType().Name} event seq no: {request.EventSequenceNumber}");

            if (AlreadyProcessed(request.EventId))
                return;


            if (!(_networkState.GetObject(request.FromNodeId) is RouteNode fromNode))
            {
                _logger.LogError($"Route network event stream seems to be broken! RouteSegmentAdded event with id: {request.EventId} and segment id: {request.SegmentId} has a FromNodeId: {request.FromNodeId} that don't exists in the current state.");
                return;
            }


            if (!(_networkState.GetObject(request.ToNodeId) is RouteNode toNode))
            {
                _logger.LogError($"Route network event stream seems to be broken! RouteSegmentAdded event with id: {request.EventId} and segment id: {request.SegmentId} has a ToNodeId: {request.ToNodeId} that don't exists in the current state.");
                return;
            }

            var envelope = GeoJsonConversionHelper.ConvertFromLineGeoJson(request.Geometry).Envelope.EnvelopeInternal;

            transaction.Add(new RouteSegment(request.SegmentId, fromNode, toNode, envelope), ignoreDublicates: true);
        }

        private void HandleEvent(RouteSegmentMarkedForDeletion request, ITransaction transaction)
        {
            _logger.LogDebug($"Handler got {request.GetType().Name} event seq no: {request.EventSequenceNumber}");

            if (AlreadyProcessed(request.EventId))
                return;

            transaction.Delete(request.SegmentId, ignoreDublicates: true);
        }

        private void HandleEvent(RouteSegmentRemoved request, ITransaction transaction)
        {
            _logger.LogDebug($"Handler got {request.GetType().Name} event seq no: {request.EventSequenceNumber}");

            if (AlreadyProcessed(request.EventId))
                return;

            transaction.Delete(request.SegmentId, ignoreDublicates: true);
        }


        private void HandleEvent(RouteNodeMarkedForDeletion request, ITransaction transaction)
        {
            _logger.LogDebug($"Handler got {request.GetType().Name} event seq no: {request.EventSequenceNumber}");

            if (AlreadyProcessed(request.EventId))
                return;

            transaction.Delete(request.NodeId, ignoreDublicates: true);
        }


        private bool AlreadyProcessed(Guid id)
        {
            if (_alreadyProcessed.Contains(id))
                return true;
            else
            {
                _alreadyProcessed.Add(id);
                return false;
            }
        }
    }
}
