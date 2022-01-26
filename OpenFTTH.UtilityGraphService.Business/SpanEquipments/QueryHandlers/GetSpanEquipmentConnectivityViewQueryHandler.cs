using DAX.ObjectVersioning.Graph;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Projections;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.QueryHandling
{
    public class GetSpanEquipmentConnectivityViewQueryHandler
        : IQueryHandler<GetSpanEquipmentConnectivityView, Result<SpanEquipmentAZConnectivityViewModel>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<SpanStructureSpecification> _spanStructureSpecifications;
        private LookupCollection<SpanEquipmentSpecification> _spanEquipmentSpecifications;

        public GetSpanEquipmentConnectivityViewQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result<SpanEquipmentAZConnectivityViewModel>> HandleAsync(GetSpanEquipmentConnectivityView query)
        {
            _spanStructureSpecifications = _eventStore.Projections.Get<SpanStructureSpecificationsProjection>().Specifications;
            _spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            List<SpanEquipmentAZConnectivityViewEquipmentInfo> spanEquipmentViewInfos = new();

            foreach (var spanEquipmentOrSegmentId in query.SpanEquipmentOrSegmentIds)
            {
                if (_utilityNetwork.TryGetEquipment<SpanEquipment>(spanEquipmentOrSegmentId, out var spanEquipment))
                {
                    spanEquipmentViewInfos.Add(BuildSpanEquipmentView(query, spanEquipment));
                }
                else
                {
                    return Task.FromResult(Result.Fail<SpanEquipmentAZConnectivityViewModel>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_EQUIPMENT_ID, $"Invalid query. Cannot find any span equipment by the equipment or span segment id specified: {spanEquipmentOrSegmentId}")));
                }
            }

            return Task.FromResult(
                      Result.Ok(
                          new SpanEquipmentAZConnectivityViewModel(
                                  spanEquipments: spanEquipmentViewInfos.ToArray()
                          )
                      )
                  );
        }

        private SpanEquipmentAZConnectivityViewEquipmentInfo BuildSpanEquipmentView(GetSpanEquipmentConnectivityView query, SpanEquipment spanEquipment)
        {
            if (!_spanEquipmentSpecifications.TryGetValue(spanEquipment.SpecificationId, out var spanEquipmentSpecification))
                throw new ApplicationException($"Invalid/corrupted span equipment instance: {spanEquipment.Id} Has reference to non-existing span equipment specification with id: {spanEquipment.SpecificationId}");

            var equipmentData = GatherRelevantSpanEquipmentData(spanEquipment);

            List<TerminalEquipmentAZConnectivityViewTerminalStructureInfo> terminalStructureInfos = new();

            int seqNo = 1;

            List<SpanEquipmentAZConnectivityViewLineInfo> lineInfos = new();

            foreach (var spanStructure in spanEquipment.SpanStructures)
            {
                var spanSegmentToTrace = GetSpanSegmentToTrace(query.RouteNetworkElementId, spanEquipment, spanStructure);

                lineInfos.Add(
                    new SpanEquipmentAZConnectivityViewLineInfo(seqNo, GetSpanStructureName(spanEquipment, spanStructure), spanSegmentToTrace.Id)
                    {
                        A = GetAEndInfo(equipmentData, spanSegmentToTrace),
                        Z = GetZEndInfo(equipmentData, spanSegmentToTrace)
                    }
                );

                seqNo++;
            }

            return (
                new SpanEquipmentAZConnectivityViewEquipmentInfo(
                       id: spanEquipment.Id,
                       category: spanEquipmentSpecification.Category,
                       name: spanEquipment.Name == null ? "NO NAME" : spanEquipment.Name,
                       specName: spanEquipmentSpecification.Name,
                       lines: lineInfos.ToArray()
                   )
             );
        }

        private string GetSpanStructureName(SpanEquipment spanEquipment, SpanStructure spanStructure)
        {
            if (!_spanStructureSpecifications.TryGetValue(spanStructure.SpecificationId, out var spanStructureSpecification))
                throw new ApplicationException($"Invalid/corrupted span equipment specification: {spanEquipment.SpecificationId} has reference to non-existing span structure specification with id: {spanStructure.SpecificationId}");

            if (spanEquipment.IsCable)
            {
                return $"Fiber {spanStructure.Position}";
            }
            else
            {
                return $"Subconduit {spanStructure.Position} ({spanStructureSpecification.Color})";
            }

        }

        private SpanSegment GetSpanSegmentToTrace(Guid routeNetworkElementId, SpanEquipment spanEquipment, SpanStructure spanStructure)
        {
            // TODO: Fix so that it uses routeNetworkElementId to 
            return spanStructure.SpanSegments.First();
        }

     

        private SpanEquipmentAZConnectivityViewEndInfo GetAEndInfo(RelevantEquipmentData relevantEquipmentData, SpanSegment spanSegment)
        {
            var traceInfo = relevantEquipmentData.TracedSegments[spanSegment.Id].A;

            var connectedToText = CreateConnectedToSpanEquipmentString(relevantEquipmentData, traceInfo);

            return new SpanEquipmentAZConnectivityViewEndInfo()
            {
                ConnectedTo = connectedToText
            };
        }

        private SpanEquipmentAZConnectivityViewEndInfo GetZEndInfo(RelevantEquipmentData relevantEquipmentData, SpanSegment spanSegment)
        {
            var traceInfo = relevantEquipmentData.TracedSegments[spanSegment.Id].Z;

            var connectedToText = CreateConnectedToSpanEquipmentString(relevantEquipmentData, traceInfo);

            return new SpanEquipmentAZConnectivityViewEndInfo()
            {
                ConnectedTo = connectedToText
            };
        }

        private string? CreateConnectedToSpanEquipmentString(RelevantEquipmentData relevantEquipmentData, TraceEndInfo? traceInfo)
        {
            if (traceInfo == null)
                return null;

            var terminalEquipment = traceInfo.NeighborTerminal.TerminalEquipment(_utilityNetwork);
            var fiber = traceInfo.NeighborTerminal.StructureIndex;

            return  $"{terminalEquipment.Name}";
        }

        private RelevantEquipmentData GatherRelevantSpanEquipmentData(SpanEquipment spanEquipment)
        {
            RelevantEquipmentData relevantEquipmentData = new RelevantEquipmentData();

            relevantEquipmentData.TracedSegments = TraceAllSegments(spanEquipment);

            var endNodesIds = GetEndNodeIdsFromTraceResult(relevantEquipmentData.TracedSegments.Values);

            relevantEquipmentData.RouteNetworkElements = GatherRelevantRouteNodeInformation(_queryDispatcher, endNodesIds);

            TryFindAandZ(relevantEquipmentData);

            return relevantEquipmentData;
        }

        private void TryFindAandZ(RelevantEquipmentData relevantEquipmentData)
        {
            foreach (var tracedTerminal in relevantEquipmentData.TracedSegments.Values)
            {
                // the lower the more A-ish
                int upstreamRank = 0;
                int downstreamRank = 1000;

                if (tracedTerminal.Upstream != null)
                {
                    var endTerminalRouteNode = relevantEquipmentData.RouteNetworkElements[tracedTerminal.Upstream.EndTerminal.RouteNodeId];

                    if (endTerminalRouteNode != null && endTerminalRouteNode.RouteNodeInfo != null && endTerminalRouteNode.RouteNodeInfo.Function != null)
                        upstreamRank = (int)endTerminalRouteNode.RouteNodeInfo.Function;
                }


                if (tracedTerminal.Downstream != null)
                {
                    var endTerminalRouteNode = relevantEquipmentData.RouteNetworkElements[tracedTerminal.Downstream.EndTerminal.RouteNodeId];

                    if (endTerminalRouteNode != null && endTerminalRouteNode.RouteNodeInfo != null && endTerminalRouteNode.RouteNodeInfo.Function != null)
                        downstreamRank = (int)endTerminalRouteNode.RouteNodeInfo.Function;
                }

                if (upstreamRank > downstreamRank)
                    tracedTerminal.UpstreamIsZ = true;
                else
                    tracedTerminal.UpstreamIsZ = false;
            }
        }

        private Dictionary<Guid, TraceInfo> TraceAllSegments(SpanEquipment spanEquipment)
        {
            Dictionary<Guid, TraceInfo> traceInfosByTerminalId = new();

            // Trace all equipment terminals
            foreach (var spanStructure in spanEquipment.SpanStructures)
            {
                foreach (var segment in spanStructure.SpanSegments)
                {
                    TraceInfo traceInfo = new TraceInfo();

                    var terminalTraceResult = _utilityNetwork.Graph.Trace(segment.Id);

                    if (terminalTraceResult != null)
                    {
                        if (terminalTraceResult.Upstream.Length > 0)
                        {
                            traceInfo.Upstream = GetEndInfoFromTrace(segment.Id, terminalTraceResult.Upstream);
                        }

                        if (terminalTraceResult.Downstream.Length > 0)
                        {
                            traceInfo.Downstream = GetEndInfoFromTrace(segment.Id, terminalTraceResult.Downstream);
                        }
                    }

                    traceInfosByTerminalId.Add(segment.Id, traceInfo);
                }
            }

            return traceInfosByTerminalId;
        }

        private IEnumerable<Guid> GetEndNodeIdsFromTraceResult(IEnumerable<TraceInfo> traceInfos)
        {
            HashSet<Guid> endNodeIds = new();

            foreach (var traceInfo in traceInfos)
            {
                AddEndNodeIdsToHash(traceInfo, endNodeIds);
            }

            return endNodeIds;
        }

        private static void AddEndNodeIdsToHash(TraceInfo traceInfo, HashSet<Guid> endNodeIds)
        {
            if (traceInfo.Upstream != null)
            {
                if (!endNodeIds.Contains(traceInfo.Upstream.EndTerminal.RouteNodeId))
                    endNodeIds.Add(traceInfo.Upstream.EndTerminal.RouteNodeId);
            }

            if (traceInfo.Downstream != null)
            {
                if (!endNodeIds.Contains(traceInfo.Downstream.EndTerminal.RouteNodeId))
                    endNodeIds.Add(traceInfo.Downstream.EndTerminal.RouteNodeId);
            }
        }

        private TraceEndInfo GetEndInfoFromTrace(Guid tracedSpanSegmentId, IGraphObject[] trace)
        {
            if (trace.Length < 2)
                throw new ApplicationException($"Expected trace length to be minimum 2. Please check trace on span segment with id: {tracedSpanSegmentId}");


            // Get neighbor terminal
            var neighborTerminal = trace.First();

            if (!(neighborTerminal is UtilityGraphConnectedTerminal))
                throw new ApplicationException($"Expected neighbor to be a UtilityGraphConnectedTerminal. Please check trace on span segment with id: {tracedSpanSegmentId}");


            // Get end terminal
            var terminalEnd = trace.Last();

            if (!(terminalEnd is UtilityGraphConnectedTerminal))
                throw new ApplicationException($"Expected end to be a UtilityGraphConnectedTerminal. Please check trace on span segment with id: {tracedSpanSegmentId}");


            return new TraceEndInfo((UtilityGraphConnectedTerminal)neighborTerminal, (UtilityGraphConnectedTerminal)terminalEnd);
        }

        private LookupCollection<RouteNetworkElement> GatherRelevantRouteNodeInformation(IQueryDispatcher queryDispatcher, IEnumerable<Guid> nodeOfInterestIds)
        {
            if (nodeOfInterestIds.Count() == 0)
                return new LookupCollection<RouteNetworkElement>();

            RouteNetworkElementIdList idList = new();
            idList.AddRange(nodeOfInterestIds);

            var interestQueryResult = queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
                new GetRouteNetworkDetails(idList)
                {
                    RouteNetworkElementFilter = new RouteNetworkElementFilterOptions() { 
                        IncludeNamingInfo = true,
                        IncludeRouteNodeInfo = true
                    }
                }
            ).Result;

            if (interestQueryResult.IsFailed)
                throw new ApplicationException("Failed to query route network information. Got error: " + interestQueryResult.Errors.First().Message);

            return interestQueryResult.Value.RouteNetworkElements;
        }

        private record RelevantEquipmentData
        {
            public Dictionary<Guid, TraceInfo> TracedSegments { get; set; }
            public LookupCollection<RouteNetworkElement> RouteNetworkElements { get; set; }
        }

        private record TraceInfo
        {
            public TraceEndInfo? Upstream { get; set; }
            public TraceEndInfo? Downstream { get; set; }

            public bool UpstreamIsZ {get; set;}
            public TraceEndInfo? Z
            {
                get
                {
                    if (UpstreamIsZ) return Upstream;
                    else return Downstream;
                }
            }

            public TraceEndInfo? A
            {
                get
                {
                    if (!UpstreamIsZ) return Upstream;
                    else return Downstream;
                }
            }

        }

        private record TraceEndInfo
        {
            public UtilityGraphConnectedTerminal NeighborTerminal { get; set; }
            public UtilityGraphConnectedTerminal EndTerminal { get; set; }

            public TraceEndInfo(UtilityGraphConnectedTerminal neighborTerminal, UtilityGraphConnectedTerminal endTerminal)
            {
                NeighborTerminal = neighborTerminal;
                EndTerminal = endTerminal;
            }
        }
    }
}
