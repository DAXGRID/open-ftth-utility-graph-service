using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.QueryHandlers.Trace
{
    public class RouteNetworkTraceResultBuilder
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public RouteNetworkTraceResultBuilder(IQueryDispatcher queryDispatcher, UtilityNetworkProjection utilityNetwork)
        {
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = utilityNetwork;
        }

        public TraceInfo? GetTraceInfo(List<SpanEquipmentWithRelatedInfo> spanEquipmentsToTrace)
        {
            if (spanEquipmentsToTrace.Count == 0)
                return null;

            var intermidiateTraceResult = GatherNetworkGraphTraceInformation(spanEquipmentsToTrace);

            if (intermidiateTraceResult.InterestList.Count > 0)
            {
                var routeNetworkInformation = GatherRouteNetworkInformation(intermidiateTraceResult.InterestList);

                if (routeNetworkInformation.Interests == null)
                    throw new ApplicationException("Failed to query route network interest information. Interest information is null");

                Dictionary<Guid, List<SpanSegmentRouteNetworkTraceRef>> traceIdRefBySpanEquipmentId = new();

                // Find unique route network traces
                List<RouteNetworkTrace> routeNetworkTraces = new();

                foreach (var segmentWalksBySpanEquipmentId in intermidiateTraceResult.SegmentWalksBySpanEquipmentId)
                {
                    foreach (var segmentWalk in segmentWalksBySpanEquipmentId.Value)
                    {
                        List<Guid> segmentIds = new();

                        foreach (var segmentHop in segmentWalk.Hops)
                        {
                            var walkIds = routeNetworkInformation.Interests[segmentHop.WalkOfInterestId].RouteNetworkElementRefs;

                            segmentIds.AddRange(GetRouteSegmentsBetweenNodes(walkIds, segmentHop.FromNodeId, segmentHop.ToNodeId));
                        }

                        Guid fromNodeId = segmentWalk.Hops.First().FromNodeId;
                        string? fromNodeName = routeNetworkInformation.RouteNetworkElements[fromNodeId].NamingInfo?.Name;

                        Guid toNodeId = segmentWalk.Hops.Last().ToNodeId;
                        string? toNodeName = routeNetworkInformation.RouteNetworkElements[toNodeId].NamingInfo?.Name;

                        Guid traceId = FindOrCreateRouteNetworkTrace(routeNetworkTraces, segmentIds, fromNodeId, toNodeId, fromNodeName, toNodeName);

                        SpanSegmentRouteNetworkTraceRef traceRef = new SpanSegmentRouteNetworkTraceRef(segmentWalk.SpanEquipmentOrSegmentId, traceId);

                        if (!traceIdRefBySpanEquipmentId.ContainsKey(segmentWalksBySpanEquipmentId.Key))
                            traceIdRefBySpanEquipmentId[segmentWalksBySpanEquipmentId.Key] = new List<SpanSegmentRouteNetworkTraceRef>() { traceRef };
                        else
                            traceIdRefBySpanEquipmentId[segmentWalksBySpanEquipmentId.Key].Add(traceRef);
                    }
                }

                return new TraceInfo(routeNetworkTraces, traceIdRefBySpanEquipmentId);
            }
            else
                return null;
        }

        private Guid FindOrCreateRouteNetworkTrace(List<RouteNetworkTrace> routeNetworkTraces, List<Guid> segmentIds, Guid fromNodeId, Guid toNodeId, string? fromNodeName, string? toNodeName)
        {
            foreach (var routeNetworkTrace in routeNetworkTraces)
            {
                if (routeNetworkTrace.RouteSegmentIds.SequenceEqual(segmentIds))
                    return routeNetworkTrace.Id;
            }

            var newRouteNetworkTrace = new RouteNetworkTrace(Guid.NewGuid(), fromNodeId, toNodeId, segmentIds.ToArray(), fromNodeName, toNodeName);

            routeNetworkTraces.Add(newRouteNetworkTrace);

            return newRouteNetworkTrace.Id;
        }

        private List<Guid> GetRouteSegmentsBetweenNodes(RouteNetworkElementIdList walkIds, Guid startNodeId, Guid endNodeId)
        {
            List<Guid> result = new();

            var startNodeIndex = walkIds.IndexOf(startNodeId);
            var endNodeIndex = walkIds.IndexOf(endNodeId);

            if (startNodeIndex < 0 || endNodeIndex < 0)
                throw new ApplicationException($"Failed to find start node: {startNodeId} or end node {endNodeId} in walk.");

            if (startNodeIndex < endNodeIndex)
            {
                for (int i = startNodeIndex + 1; i < endNodeIndex; i+=2)
                {
                    result.Add(walkIds[i]);
                }
            }
            else
            {
                for (int i = startNodeIndex - 1; i > endNodeIndex; i-=2)
                {
                    result.Add(walkIds[i]);
                }
            }

            return result;
        }

        private GetRouteNetworkDetailsResult GatherRouteNetworkInformation(IEnumerable<Guid> walkOfInterestIds)
        {
            InterestIdList interestIdList = new();
            interestIdList.AddRange(walkOfInterestIds);

            var interestQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
                new GetRouteNetworkDetails(interestIdList)
                {
                    RouteNetworkElementFilter = new RouteNetworkElementFilterOptions() { IncludeNamingInfo = true }
                }
            ).Result;

            if (interestQueryResult.IsFailed)
                throw new ApplicationException("Failed to query route network information. Got error: " + interestQueryResult.Errors.First().Message);
       

            return interestQueryResult.Value;
        }


        private IntermidiateTraceResult GatherNetworkGraphTraceInformation(List<SpanEquipmentWithRelatedInfo> spanEquipmentsToTrace)
        {
            IntermidiateTraceResult result = new();

            // Trace all segments of all span equipments
            foreach (var spanEquipment in spanEquipmentsToTrace)
            {
                // Add walk that covers the whole span equipment
                var spanEquipmentSegmentHop = new SegmentWalkHop()
                {
                    FromNodeId = spanEquipment.NodesOfInterestIds.First(),
                    ToNodeId = spanEquipment.NodesOfInterestIds.Last(),
                    WalkOfInterestId = spanEquipment.WalkOfInterestId
                };

                // Snatch walk of interest id
                if (!result.InterestList.Contains(spanEquipment.WalkOfInterestId))
                    result.InterestList.Add(spanEquipment.WalkOfInterestId);

                result.SegmentWalksBySpanEquipmentId.Add(spanEquipment.Id, new List<SegmentWalk> { new SegmentWalk(spanEquipment.Id) { Hops = new List<SegmentWalkHop>() { spanEquipmentSegmentHop } } });

                // Add walks for each span segment in the span equipment
                foreach (var spanStructure in spanEquipment.SpanStructures)
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        var spanTraceResult = _utilityNetwork.Graph.TraceSegment(spanSegment.Id);

                        // We're dealing with a connected segment if non-empty trace result is returned
                        if (spanTraceResult.Upstream.Length > 0)
                        {
                            var segmentWalk = new SegmentWalk(spanSegment.Id);

                            for (int downstreamIndex = spanTraceResult.Downstream.Length - 1; downstreamIndex > 0; downstreamIndex--)
                            {
                                var item = spanTraceResult.Downstream[downstreamIndex];

                                if (item is UtilityGraphConnectedSegment connectedSegment)
                                {
                                    // Snatch walk of interest id
                                    Guid walkOfInterestId = AddWalkOfInterestToResult(result, connectedSegment);

                                    var segmentHop = new SegmentWalkHop()
                                    {
                                        FromNodeId = ((UtilityGraphConnectedTerminal)spanTraceResult.Downstream[downstreamIndex + 1]).NodeOfInterestId,
                                        ToNodeId = ((UtilityGraphConnectedTerminal)spanTraceResult.Downstream[downstreamIndex - 1]).NodeOfInterestId,
                                        WalkOfInterestId = walkOfInterestId
                                    };

                                    segmentWalk.Hops.Add(segmentHop);
                                }
                            }

                            for (int upstreamIndex = 0; upstreamIndex < spanTraceResult.Upstream.Length; upstreamIndex++)
                            {
                                var item = spanTraceResult.Upstream[upstreamIndex];

                                if (item is UtilityGraphConnectedSegment connectedSegment)
                                {
                                    // Snatch walk of interest id
                                    var walkOfInterestId = AddWalkOfInterestToResult(result, connectedSegment);

                                    if (upstreamIndex == 0)
                                    {
                                        var segmentHop = new SegmentWalkHop()
                                        {
                                            FromNodeId = ((UtilityGraphConnectedTerminal)spanTraceResult.Downstream[1]).NodeOfInterestId,
                                            ToNodeId = ((UtilityGraphConnectedTerminal)spanTraceResult.Upstream[upstreamIndex + 1]).NodeOfInterestId,
                                            WalkOfInterestId = walkOfInterestId
                                        };

                                        segmentWalk.Hops.Add(segmentHop);
                                    }
                                    else
                                    {
                                        var segmentHop = new SegmentWalkHop()
                                        {
                                            FromNodeId = ((UtilityGraphConnectedTerminal)spanTraceResult.Upstream[upstreamIndex - 1]).NodeOfInterestId,
                                            ToNodeId = ((UtilityGraphConnectedTerminal)spanTraceResult.Upstream[upstreamIndex + 1]).NodeOfInterestId,
                                            WalkOfInterestId = walkOfInterestId
                                        };

                                        segmentWalk.Hops.Add(segmentHop);
                                    }
                                }
                            }

                            AddWalkToResult(result, spanEquipment, segmentWalk);
                        }
                        // We're dealing with an unconnected segment
                        else
                        {
                            if (_utilityNetwork.Graph.TryGetGraphElement<UtilityGraphDisconnectedSegment>(spanSegment.Id, out var disconnectedSegment))
                            {
                                var disconnectedSpanSegment = disconnectedSegment.SpanSegment(_utilityNetwork);

                                // if disconnected segment has been cut
                                if (disconnectedSpanSegment.FromNodeOfInterestIndex > 0 || disconnectedSpanSegment.ToNodeOfInterestIndex < (spanEquipment.NodesOfInterestIds.Length - 1))
                                {
                                    var segmentHop = new SegmentWalkHop()
                                    {
                                        FromNodeId = spanEquipment.NodesOfInterestIds[disconnectedSpanSegment.FromNodeOfInterestIndex],
                                        ToNodeId = spanEquipment.NodesOfInterestIds[disconnectedSpanSegment.ToNodeOfInterestIndex],
                                        WalkOfInterestId = spanEquipment.WalkOfInterestId
                                    };

                                    var segmentWalk = new SegmentWalk(spanSegment.Id);

                                    segmentWalk.Hops.Add(segmentHop);
                                    AddWalkToResult(result, spanEquipment, segmentWalk);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static void AddWalkToResult(IntermidiateTraceResult result, SpanEquipmentWithRelatedInfo spanEquipment, SegmentWalk segmentWalk)
        {
            if (!result.SegmentWalksBySpanEquipmentId.ContainsKey(spanEquipment.Id))
                result.SegmentWalksBySpanEquipmentId[spanEquipment.Id] = new List<SegmentWalk>() { segmentWalk };
            else
                result.SegmentWalksBySpanEquipmentId[spanEquipment.Id].Add(segmentWalk);
        }

        private Guid AddWalkOfInterestToResult(IntermidiateTraceResult result, UtilityGraphConnectedSegment connectedSegment)
        {
            var walkOfInterestId = connectedSegment.SpanEquipment(_utilityNetwork).WalkOfInterestId;

            if (!result.InterestList.Contains(walkOfInterestId))
                result.InterestList.Add(walkOfInterestId);
            return walkOfInterestId;
        }

        private class IntermidiateTraceResult
        {
            public HashSet<Guid> InterestList = new();
            public Dictionary<Guid, List<SegmentWalk>> SegmentWalksBySpanEquipmentId = new();
        }

        private record SegmentWalk
        {
            public Guid SpanEquipmentOrSegmentId { get; }

            public List<SegmentWalkHop> Hops = new();

            public SegmentWalk(Guid spanEquipmentOrSegmentId)
            {
                SpanEquipmentOrSegmentId = spanEquipmentOrSegmentId;
            }
        }

        private record SegmentWalkHop
        {
            public Guid FromNodeId { get; set; }
            public Guid ToNodeId { get; set; }
            public Guid WalkOfInterestId { get; set; }
        }
    }

    public class TraceInfo
    {
        public List<RouteNetworkTrace> RouteNetworkTraces { get; }
        public Dictionary<Guid, List<SpanSegmentRouteNetworkTraceRef>> SpanSegmentRouteNetworkTraceRefsBySpanEquipmentId { get; }

        public TraceInfo(List<RouteNetworkTrace> routeNetworkTraces, Dictionary<Guid, List<SpanSegmentRouteNetworkTraceRef>> spanSegmentRouteNetworkTraceRefsBySpanEquipmentId)
        {
            RouteNetworkTraces = routeNetworkTraces;
            SpanSegmentRouteNetworkTraceRefsBySpanEquipmentId = spanSegmentRouteNetworkTraceRefsBySpanEquipmentId;
        }
    }
}
