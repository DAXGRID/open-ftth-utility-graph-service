using DAX.ObjectVersioning.Graph;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.Graph.Trace;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.Trace.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.Trace.QueryHandling
{
    public class GetConnectivityTraceQueryHandler
        : IQueryHandler<GetConnectivityTraceView, Result<ConnectivityTraceView>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;


        public GetConnectivityTraceQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
        }

        public Task<Result<ConnectivityTraceView>> HandleAsync(GetConnectivityTraceView query)
        {
            if (_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(query.TerminalOrSpanSegmentId, out var utilityGraphTerminalRef))
            {
                return Task.FromResult(BuildTraceViewFromTerminal(utilityGraphTerminalRef));
            }
            else if (_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(query.TerminalOrSpanSegmentId, out var utilityGraphSegmentRef))
            {
                return Task.FromResult(BuildTraceViewFromSegment(query.TerminalOrSpanSegmentId, utilityGraphSegmentRef));
            }

            return Task.FromResult(NotConnected());
        }

        private Result<ConnectivityTraceView> BuildTraceViewFromTerminal(IUtilityGraphTerminalRef sourceTerminalRef)
        {
            var traceResult = _utilityNetwork.Graph.Trace(sourceTerminalRef.TerminalId);

            List<IGraphObject> traceElements = new();

            if (traceResult.Upstream.Length > 0)
                traceElements.AddRange(traceResult.Upstream.Reverse());
            if (traceResult.Source != null)
                traceElements.Add(traceResult.Source);
            if (traceResult.Downstream.Length > 0)
                traceElements.AddRange(traceResult.Downstream);

            var addressIds = GetAddressIdsFromTraceResult(traceResult);

            var relatedData = new RelatedDataHolder(_eventStore, _utilityNetwork, _queryDispatcher, traceElements.OfType<IUtilityGraphTerminalRef>().Select(t => t.RouteNodeId).ToArray(), addressIds);

            var terminalEquipment = sourceTerminalRef.TerminalEquipment(_utilityNetwork);

            ReverseIfNeeded(traceElements, relatedData.RouteNetworkElementById);

            List<ConnectivityTraceViewHopInfo> hops = new();

            int hopSeqNo = 1;

            for (int graphElementIndex = 0; graphElementIndex < traceElements.Count; graphElementIndex++)
            {
                var currentGraphElement = traceElements[graphElementIndex];

                if (currentGraphElement is IUtilityGraphTerminalRef terminalRef)
                {
                    string connectionInfo = GetConnectionInfo(relatedData, traceElements, graphElementIndex);

                    hops.Add(
                        new ConnectivityTraceViewHopInfo(
                            hopSeqNo,
                            level: 0,
                            isSplitter: false,
                            isTraceSource: false,
                            node: terminalRef.IsDummyEnd ? relatedData.GetNodeName(terminalRef.RouteNodeId) : relatedData.GetNodeOrEquipmentName(terminalRef.RouteNodeId, terminalRef.TerminalEquipment(_utilityNetwork)),
                            equipment: terminalRef.IsDummyEnd ? "løs ende" : relatedData.GetCompactEquipmentWithTypeInfoString(terminalRef.RouteNodeId, terminalRef.TerminalEquipment(_utilityNetwork)),
                            terminalStructure: relatedData.GetEquipmentStructureInfoString(terminalRef),
                            terminal: relatedData.GetEquipmentTerminalInfoString(terminalRef),
                            connectionInfo: connectionInfo,
                            totalLength: 2,
                            routeSegmentGeometries: Array.Empty<string>(),
                            routeSegmentIds: Array.Empty<Guid>()
                        )
                    );

                    hopSeqNo++;
                }
            }

            return Result.Ok(new ConnectivityTraceView("FK000000", hops.ToArray()));

        }

        private Result<ConnectivityTraceView> BuildTraceViewFromSegment(Guid spanSegmentId, IUtilityGraphSegmentRef utilityGraphSegmentRef)
        {
            var traceResult = _utilityNetwork.Graph.Trace(spanSegmentId);

            List<IGraphObject> traceElements = traceResult.All;

            var addressIds = GetAddressIdsFromTraceResult(traceResult);

            var relatedData = new RelatedDataHolder(_eventStore, _utilityNetwork, _queryDispatcher, traceElements.OfType<IUtilityGraphTerminalRef>().Select(t => t.RouteNodeId).ToArray(), addressIds);

            ReverseIfNeeded(traceElements, relatedData.RouteNetworkElementById);

            List<ConnectivityTraceViewHopInfo> hops = new();

            int hopSeqNo = 1;

            for (int graphElementIndex = 0; graphElementIndex < traceElements.Count; graphElementIndex++)
            {
                var currentGraphElement = traceElements[graphElementIndex];

                if (currentGraphElement is IUtilityGraphTerminalRef terminalRef)
                {
                    string connectionInfo = GetConnectionInfo(relatedData, traceElements, graphElementIndex);

                    hops.Add(
                        new ConnectivityTraceViewHopInfo(
                            hopSeqNo,
                            level: 0,
                            isSplitter: false,
                            isTraceSource: false,
                            node: terminalRef.IsDummyEnd ? relatedData.GetNodeName(terminalRef.RouteNodeId) : relatedData.GetNodeOrEquipmentName(terminalRef.RouteNodeId, terminalRef.TerminalEquipment(_utilityNetwork)),
                            equipment: terminalRef.IsDummyEnd ? "løs ende" : relatedData.GetCompactEquipmentWithTypeInfoString(terminalRef.RouteNodeId, terminalRef.TerminalEquipment(_utilityNetwork)),
                            terminalStructure: relatedData.GetEquipmentStructureInfoString(terminalRef),
                            terminal: relatedData.GetEquipmentTerminalInfoString(terminalRef),
                            connectionInfo: connectionInfo,
                            totalLength: 2,
                            routeSegmentGeometries: Array.Empty<string>(),
                            routeSegmentIds: Array.Empty<Guid>()
                        )
                    );

                    hopSeqNo++;
                }
            }

            return Result.Ok(new ConnectivityTraceView("FK000000", hops.ToArray()));

        }

        private string GetConnectionInfo(RelatedDataHolder relatedData, List<IGraphObject> traceElements, int graphElementIndex)
        {
            // If segment follow terminal, then write span segment information in connection info
            if (graphElementIndex < (traceElements.Count - 1))
            {
                return GetSpanConnectionInfo(relatedData, traceElements[graphElementIndex + 1] as IUtilityGraphSegmentRef);
            }
            else
            {
                var terminalRef = traceElements[graphElementIndex] as IUtilityGraphTerminalRef;

                if (terminalRef != null && !terminalRef.IsDummyEnd)
                {
                    var addressId = GetTerminalEquipmentMostAccurateAddressId(terminalRef.TerminalEquipment(_utilityNetwork));

                    if (addressId != null)
                    {
                        var addressString = relatedData.GetAddressString(addressId);
                        if (addressString != null)
                            return addressString;
                    }
                }
            }

            return "";
        }

        private Guid? GetTerminalEquipmentMostAccurateAddressId(TerminalEquipment terminalEquipment)
        {
            if (terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.UnitAddressId != null)
                return terminalEquipment.AddressInfo.UnitAddressId.Value;
            else if (terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.AccessAddressId != null)
                return terminalEquipment.AddressInfo.AccessAddressId.Value;

            return null;
        }

        private string GetSpanConnectionInfo(RelatedDataHolder relatedData, IUtilityGraphSegmentRef? utilityGraphSegmentRef)
        {
            var spanEquipment = utilityGraphSegmentRef.SpanEquipment(_utilityNetwork);

            return relatedData.GetSpanEquipmentFullFiberCableString(spanEquipment, utilityGraphSegmentRef.StructureIndex);
        }

        private HashSet<Guid> GetAddressIdsFromTraceResult(UtilityGraphTraceResult trace)
        {
            HashSet<Guid> addressIds = new();

            if (trace == null)
                return addressIds;

            foreach (var graphObject in trace.All)
            {
                if (graphObject is IUtilityGraphTerminalRef)
                {
                    var terminalRef = (IUtilityGraphTerminalRef)graphObject;

                    if (!terminalRef.IsDummyEnd)
                    {
                        var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

                        if (terminalEquipment.AddressInfo != null)
                        {
                            if (terminalEquipment.AddressInfo.UnitAddressId != null && terminalEquipment.AddressInfo.UnitAddressId != Guid.Empty)
                                addressIds.Add(terminalEquipment.AddressInfo.UnitAddressId.Value);
                            else if (terminalEquipment.AddressInfo.AccessAddressId != null && terminalEquipment.AddressInfo.AccessAddressId != Guid.Empty)
                                addressIds.Add(terminalEquipment.AddressInfo.AccessAddressId.Value);
                        }
                    }

                }
            }

            return addressIds;
        }

        private List<IGraphObject> ReverseIfNeeded(List<IGraphObject> trace, Dictionary<Guid, RouteNetworkElement> routeNetworkElements)
        {
            var terminals = trace.OfType<IUtilityGraphTerminalRef>();

            if (terminals.Count() > 1)
            {
                var currentFromNode = routeNetworkElements[terminals.First().RouteNodeId];
                var currentToNode = routeNetworkElements[terminals.Last().RouteNodeId];

                // the lower the more A-ish
                int upstreamRank;
                if (currentFromNode != null && currentFromNode.RouteNodeInfo != null && currentFromNode.RouteNodeInfo.Function != null)
                    upstreamRank = (int)currentFromNode.RouteNodeInfo.Function;
                else
                    upstreamRank = 1000; // Simple node with no function specificed get the high value (equal low score for A)

                int downstreamRank;

                if (currentToNode != null && currentToNode.RouteNodeInfo != null && currentToNode.RouteNodeInfo.Function != null)
                    downstreamRank = (int)currentToNode.RouteNodeInfo.Function;
                else
                    downstreamRank = 1000; // Simple node with no function node specified get the high value (equal low score for A)

                if (upstreamRank > downstreamRank)
                {
                    trace.Reverse();
                    return trace;
                }
            }

            return trace;
        }

        private Result<ConnectivityTraceView> NotConnected()
        {
            return Result.Ok(new ConnectivityTraceView("Not connected", new ConnectivityTraceViewHopInfo[] { }));
        }
    }
}
