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

        public TraceInfo GetTraceInfo(List<SpanEquipmentWithRelatedInfo> spanEquipmentsToTrace)
        {
            var intermidiateTraceResult = GatherNetworkGraphTraceInformation(spanEquipmentsToTrace);

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

                result.SegmentWalksBySpanEquipmentId.Add(spanEquipment.Id, new List<SegmentWalk> { new SegmentWalk { SpanEquipmentOrSegmentId = spanEquipment.Id, Hops = new List<SegmentWalkHop>() { spanEquipmentSegmentHop } } });

                // Add walks for each span segment in the span equipment
                foreach (var spanStructure in spanEquipment.SpanStructures)
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        var spanTraceResult = _utilityNetwork.Graph.TraceSegment(spanSegment.Id);

                        if (spanTraceResult.Upstream.Length > 0)
                        {
                            SegmentWalk segmentWalk = new();
                            segmentWalk.SpanEquipmentOrSegmentId = spanSegment.Id;

                            for (int downstreamIndex = spanTraceResult.Downstream.Length - 1; downstreamIndex > 0; downstreamIndex--)
                            {
                                var item = spanTraceResult.Downstream[downstreamIndex];

                                if (item is UtilityGraphConnectedSegment connectedSegment)
                                {
                                    // Snatch walk of interest id
                                    var walkOfInterestId = connectedSegment.SpanEquipment(_utilityNetwork).WalkOfInterestId;

                                    if (!result.InterestList.Contains(walkOfInterestId))
                                        result.InterestList.Add(walkOfInterestId);

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
                                    var walkOfInterestId = connectedSegment.SpanEquipment(_utilityNetwork).WalkOfInterestId;
                                    if (!result.InterestList.Contains(walkOfInterestId))
                                        result.InterestList.Add(walkOfInterestId);

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

                            if (!result.SegmentWalksBySpanEquipmentId.ContainsKey(spanEquipment.Id))
                                result.SegmentWalksBySpanEquipmentId[spanEquipment.Id] = new List<SegmentWalk>() { segmentWalk };
                            else
                                result.SegmentWalksBySpanEquipmentId[spanEquipment.Id].Add(segmentWalk);
                        }
                    }
                }
            }

            return result;
        }


        private class IntermidiateTraceResult
        {
            public HashSet<Guid> InterestList = new();
            public Dictionary<Guid, List<SegmentWalk>> SegmentWalksBySpanEquipmentId = new();
        }

        private record SegmentWalk
        {
            public Guid SpanEquipmentOrSegmentId;

            public List<SegmentWalkHop> Hops = new();
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
