using DAX.ObjectVersioning.Core;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Trace;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraph
    {
        private readonly UtilityNetworkProjection _utilityNetworkProjection;

        private ConcurrentDictionary<Guid, IUtilityGraphElement> _graphElementsById = new ConcurrentDictionary<Guid, IUtilityGraphElement>();

        private InMemoryObjectManager _objectManager = new InMemoryObjectManager();
        

        public UtilityGraph(UtilityNetworkProjection utilityNetworkProjection)
        {
            _utilityNetworkProjection = utilityNetworkProjection;
        }

        public bool TryGetGraphElement<T>(Guid id, out T utilityGraphElement) where T : IUtilityGraphElement
        {
            if (_graphElementsById.TryGetValue(id, out var graphElement))
            {
                if (graphElement is T)
                {
                    utilityGraphElement = (T)graphElement;
                    return true;
                }
            }

            #pragma warning disable CS8601 // Possible null reference assignment.
            utilityGraphElement = default(T);
            #pragma warning restore CS8601 // Possible null reference assignment.

            return false;
        }
       

        public SpanSegmentTraceResult TraceSegment(Guid id)
        {
            var result = new SpanSegmentTraceResult() { SpanSegmentId = id };

            if (_graphElementsById.TryGetValue(id, out var utilityGraphElement))
            {
                if (utilityGraphElement is UtilityGraphDisconnectedSegment)
                {
                    var disconnectedSegment = utilityGraphElement as UtilityGraphDisconnectedSegment;
                }
                else if (utilityGraphElement is UtilityGraphConnectedSegment)
                {
                    var connectedSegment = (UtilityGraphConnectedSegment)utilityGraphElement;

                    var version = _objectManager.GetLatestCommitedVersion();

                    // Do the upstream trace
                    var upstreamTrace = connectedSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                        version,
                        n => n != connectedSegment.InV(version)
                    ).ToList();

                    var lastUpstreamObject = upstreamTrace.Last();

                    if (lastUpstreamObject is UtilityGraphConnectedSegment lastUpstreamSegment)
                    {
                        if (lastUpstreamSegment.OutV(version) == null)
                        {
                            upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.NewGuid(), lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
                        }
                        else if (lastUpstreamSegment.InV(version) == null)
                        {
                            upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.NewGuid(), lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                        }
                        else
                        {
                            throw new ApplicationException($"Last element in upstream trace was a UtilityGraphConnectedSegment with id: {lastUpstreamSegment.Id}, not a terminal. However, the segment seems to have to have an upstream terminal connection. Something wrong!");
                        }
                    }

                    result.Upstream = upstreamTrace.ToArray();

                    // Do the downstream trace
                    var downstreamTrace = connectedSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                        version,
                        n => n != connectedSegment.OutV(version)
                    ).ToList();

                    var lastDownstreamObject = downstreamTrace.Last();

                    if (lastDownstreamObject is UtilityGraphConnectedSegment lastDownstreamSegment)
                    {
                        if (lastDownstreamSegment.InV(version) == null)
                        {
                            downstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.NewGuid(), lastDownstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastDownstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                        }
                        else if (lastDownstreamSegment.OutV(version) == null)
                        {
                            downstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.NewGuid(), lastDownstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastDownstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
                        }
                        else
                        {
                            throw new ApplicationException($"Last element in downstream trace was a UtilityGraphConnectedSegment with id: {lastDownstreamSegment.Id}, not a terminal. However, the segment seems to have to have an downstream terminal connection. Something wrong!");
                        }
                    }

                    result.Downstream = downstreamTrace.ToArray();
                }
            }

            return result;
        }


        internal UtilityGraphConnectedTerminal? GetTerminal(Guid terminalId, long version)
        {
            // Find or create terminal
            return _objectManager.GetObject(terminalId, version) as UtilityGraphConnectedTerminal;
        }

        internal void AddDisconnectedSegment(SpanEquipment spanEquipment, UInt16 structureIndex, UInt16 segmentIndex)
        {
            var spanSegment = spanEquipment.SpanStructures[structureIndex].SpanSegments[segmentIndex];

            var disconnectedGraphSegment = new UtilityGraphDisconnectedSegment(spanEquipment.Id, structureIndex, segmentIndex);

            if (!_graphElementsById.TryAdd(spanSegment.Id, disconnectedGraphSegment))
                throw new ArgumentException($"A span segment with id: {spanSegment.Id} already exists in the graph.");
        }

        internal void RemoveDisconnectedSegment(Guid spanSegmentId)
        {
            if (!_graphElementsById.TryRemove(spanSegmentId, out _))
                throw new ArgumentException($"The span segment with id: {spanSegmentId} cannot be removed from the graph.");
        }

        internal void UpdateIndex(Guid segmentId, IUtilityGraphSegmentRef newUtilityGraphSegmentRef)
        {
            RemoveFromIndex(segmentId);
            AddToIndex(segmentId, newUtilityGraphSegmentRef);
        }

        internal void RemoveFromIndex(Guid segmentId)
        {
            if (!_graphElementsById.TryRemove(segmentId, out _))
                throw new ApplicationException($"Cannot remove segment with id: {segmentId} from graph.");
        }

        internal void AddToIndex(Guid segmentId, IUtilityGraphSegmentRef newUtilityGraphSegmentRef)
        {
            if (!_graphElementsById.TryAdd(segmentId, newUtilityGraphSegmentRef))
                throw new ArgumentException($"A span segment with id: {segmentId} already exists in the graph.");
        }

        internal ITransaction CreateTransaction()
        {
            return _objectManager.CreateTransaction();
        }
    }
}
