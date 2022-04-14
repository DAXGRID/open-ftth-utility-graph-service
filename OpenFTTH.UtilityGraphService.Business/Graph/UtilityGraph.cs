using DAX.ObjectVersioning.Core;
using DAX.ObjectVersioning.Graph;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraph
    {
        private readonly UtilityNetworkProjection _utilityNetworkProjection;

        private ConcurrentDictionary<Guid, IUtilityGraphElement> _graphElementsById = new ConcurrentDictionary<Guid, IUtilityGraphElement>();

        private InMemoryObjectManager _objectManager = new InMemoryObjectManager();
        
        public long LatestCommitedVersion => _objectManager.GetLatestCommitedVersion();

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
       

        public UtilityGraphTraceResult Trace(Guid id)
        {

            if (_graphElementsById.TryGetValue(id, out var utilityGraphElement))
            {
                if (utilityGraphElement is UtilityGraphDisconnectedSegment)
                {
                    return new UtilityGraphTraceResult(id, null, Array.Empty<IGraphObject>(), Array.Empty<IGraphObject>());
                }
                else if (utilityGraphElement is UtilityGraphConnectedSegment)
                {
                    var connectedSegment = (UtilityGraphConnectedSegment)utilityGraphElement;

                    var version = _objectManager.GetLatestCommitedVersion();

                    var upstream = UpstreamSegmentTrace(connectedSegment, version).ToArray();
                    var downstream = DownstreamSegmentTrace(connectedSegment, version).ToArray();

                    return new UtilityGraphTraceResult(id, connectedSegment, downstream, upstream);
                }
                else if (utilityGraphElement is IUtilityGraphTerminalRef)
                {
                    IUtilityGraphTerminalRef terminalRef = (IUtilityGraphTerminalRef)utilityGraphElement;

                    var version = _objectManager.GetLatestCommitedVersion();

                    var terminal = (UtilityGraphConnectedTerminal)_objectManager.GetObject(terminalRef.TerminalId);

                    if (terminal != null)
                    {
                        var nTerminalNeigbours = terminal.NeighborElements(version).Count;

                        if (nTerminalNeigbours == 1)
                        {
                            var upstream = UpstreamTerminalTrace(terminal, version).ToArray();
                            return new UtilityGraphTraceResult(id, terminal, Array.Empty<IGraphObject>(), upstream);
                        }
                        else if (nTerminalNeigbours == 2)
                        {
                            var upstream = UpstreamTerminalTrace(terminal, version).ToArray();
                            var downstream = DownstreamTerminalTrace(terminal, version).ToArray();

                            return new UtilityGraphTraceResult(id, terminal, downstream, upstream);
                        }
                        else if (nTerminalNeigbours > 2)
                        {
                            throw new ApplicationException($"terminal with id: {terminal.Id} version: {version} have more than two segment connected to it. The system must prevent that to never happend!");
                        }
                    }
                }
            }

            return new UtilityGraphTraceResult(id, null, Array.Empty<IGraphObject>(), Array.Empty<IGraphObject>());
        }
       

        private List<IGraphObject> DownstreamSegmentTrace(UtilityGraphConnectedSegment connectedSegment, long version)
        {
            var downstreamTrace = connectedSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                version,
                n => n != connectedSegment.OutV(version)
            ).ToList();

            var lastDownstreamObject = downstreamTrace.Last();

            if (lastDownstreamObject is UtilityGraphConnectedSegment lastDownstreamSegment)
            {
                if (lastDownstreamSegment.InV(version) == null)
                {
                    downstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastDownstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastDownstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                }
                else if (lastDownstreamSegment.OutV(version) == null)
                {
                    downstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastDownstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastDownstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
                }
                else
                {
                    throw new ApplicationException($"Last element in downstream trace was a UtilityGraphConnectedSegment with id: {lastDownstreamSegment.Id}, not a terminal. However, the segment seems to have to have an downstream terminal connection. Something wrong!");
                }
            }

            return downstreamTrace;
        }

        private List<IGraphObject> DownstreamTerminalTrace(UtilityGraphConnectedTerminal terminal, long version)
        {
            var lastSegment = terminal.NeighborElements(version).Last();

            var downstreamTrace = lastSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                version,
                n => n != terminal
            ).ToList();

            var lastDownstreamObject = downstreamTrace.Last();

            if (lastDownstreamObject is UtilityGraphConnectedSegment lastDownstreamSegment)
            {
                if (lastDownstreamSegment.InV(version) == null)
                {
                    downstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastDownstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastDownstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                }
                else if (lastDownstreamSegment.OutV(version) == null)
                {
                    downstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastDownstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastDownstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
                }
                else
                {
                    throw new ApplicationException($"Last element in downstream trace was a UtilityGraphConnectedSegment with id: {lastDownstreamSegment.Id}, not a terminal. However, the segment seems to have to have an downstream terminal connection. Something wrong!");
                }
            }

            return downstreamTrace;
        }

        private List<IGraphObject> UpstreamSegmentTrace(UtilityGraphConnectedSegment connectedSegment, long version)
        {
            var upstreamTrace = connectedSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                version,
                n => n != connectedSegment.InV(version)
            ).ToList();

            var lastUpstreamObject = upstreamTrace.Last();

            if (lastUpstreamObject is UtilityGraphConnectedSegment lastUpstreamSegment)
            {
                if (lastUpstreamSegment.OutV(version) == null)
                {
                    upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
                }
                else if (lastUpstreamSegment.InV(version) == null)
                {
                    upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                }
                else
                {
                    throw new ApplicationException($"Last element in upstream trace was a UtilityGraphConnectedSegment with id: {lastUpstreamSegment.Id}, not a terminal. However, the segment seems to have to have an upstream terminal connection. Something wrong!");
                }
            }

            return upstreamTrace;
        }

        private List<IGraphObject> UpstreamTerminalTrace(UtilityGraphConnectedTerminal terminal, long version)
        {
            var firstSegment = terminal.NeighborElements(version).First();

            var upstreamTrace = firstSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                version,
                n => n != terminal
            ).ToList();

            var lastUpstreamObject = upstreamTrace.Last();

            if (lastUpstreamObject is UtilityGraphConnectedSegment lastUpstreamSegment)
            {
                if (lastUpstreamSegment.OutV(version) == null)
                {
                    upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
                }
                else if (lastUpstreamSegment.InV(version) == null)
                {
                    upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.Empty, Guid.Empty, lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                }
                else
                {
                    throw new ApplicationException($"Last element in upstream trace was a UtilityGraphConnectedSegment with id: {lastUpstreamSegment.Id}, not a terminal. However, the segment seems to have to have an upstream terminal connection. Something wrong!");
                }
            }

            return upstreamTrace;
        }

        internal IUtilityGraphTerminalRef? GetTerminal(Guid terminalId, long version)
        {
            // Find or create terminal
            return _objectManager.GetObject(terminalId, version) as IUtilityGraphTerminalRef;
        }

        internal void AddDisconnectedSegment(SpanEquipment spanEquipment, UInt16 structureIndex, UInt16 segmentIndex)
        {
            var spanSegment = spanEquipment.SpanStructures[structureIndex].SpanSegments[segmentIndex];

            var disconnectedGraphSegment = new UtilityGraphDisconnectedSegment(spanEquipment.Id, structureIndex, segmentIndex);

            if (!_graphElementsById.TryAdd(spanSegment.Id, disconnectedGraphSegment))
                throw new ArgumentException($"A span segment with id: {spanSegment.Id} already exists in the graph.");
        }

        internal void AddDisconnectedTerminal(Guid routeNodeId, TerminalEquipment terminalEquipment, Guid terminalId, UInt16 structureIndex, UInt16 terminalIndex)
        {
            var terminal = terminalEquipment.TerminalStructures[structureIndex].Terminals[terminalIndex];

            var disconnectedGraphTerminal = new UtilityGraphDisconnectedTerminal(routeNodeId, terminalId, terminalEquipment.Id, structureIndex, terminalIndex);

            if (!_graphElementsById.TryAdd(terminal.Id, disconnectedGraphTerminal))
                throw new ArgumentException($"A terminal with id: {terminal.Id} already exists in the graph.");
        }

        internal void RemoveGraphElement(Guid graphElementId)
        {
            if (!_graphElementsById.TryRemove(graphElementId, out _))
                throw new ArgumentException($"The graph element with id: {graphElementId} cannot be removed from the graph.");
        }

        internal void UpdateIndex(Guid graphElementId, IUtilityGraphElement newUtilityGraphElementRef)
        {
            RemoveFromIndex(graphElementId);
            AddToIndex(graphElementId, newUtilityGraphElementRef);
        }

        internal void RemoveFromIndex(Guid segmentId)
        {
            if (!_graphElementsById.TryRemove(segmentId, out _))
                throw new ApplicationException($"Cannot remove graph element with id: {segmentId} from graph.");
        }

        internal void AddToIndex(Guid graphElementId, IUtilityGraphElement newUtilityGraphElement)
        {
            if (!_graphElementsById.TryAdd(graphElementId, newUtilityGraphElement))
                throw new ArgumentException($"A graph element with id: {graphElementId} already exists in the graph.");
        }

        internal ITransaction CreateTransaction()
        {
            return _objectManager.CreateTransaction();
        }
    }
}
