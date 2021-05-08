using DAX.ObjectVersioning.Core;
using FluentResults;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Trace;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
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

        public void AddDisconnectedSegment(SpanEquipment spanEquipment, UInt16 structureIndex, UInt16 segmentIndex)
        {
            var spanSegment = spanEquipment.SpanStructures[structureIndex].SpanSegments[segmentIndex];

            var disconnectedGraphSegment = new UtilityGraphDisconnectedSegment(spanEquipment.Id, structureIndex, segmentIndex);

            if (!_graphElementsById.TryAdd(spanSegment.Id, disconnectedGraphSegment))
                throw new ArgumentException($"A span segment with id: {spanSegment.Id} already exists in the graph.");
        }

        public void RemoveDisconnectedSegment(Guid spanSegmentId)
        {
            if (!_graphElementsById.TryRemove(spanSegmentId, out _))
                throw new ArgumentException($"The span segment with id: {spanSegmentId} cannot be removed from the graph.");
        }


        public void ApplySegmentCut(SpanEquipment spanEquipment, SpanSegmentCutInfo spanSegmentCutInfo)
        {
            var version = _objectManager.GetLatestCommitedVersion();

            var trans = _objectManager.CreateTransaction();

            try
            {
                if (!_graphElementsById.TryGetValue(spanSegmentCutInfo.OldSpanSegmentId, out var oldSegmentGraphElement))
                    throw new ApplicationException($"Cannot find span segment graph element with id: {spanSegmentCutInfo.OldSpanSegmentId} processing segment cut.");

                if (!spanEquipment.TryGetSpanSegment(spanSegmentCutInfo.NewSpanSegmentId1, out var segment1withIndexInfo))
                    throw new ApplicationException($"Cannot find span segment with id: {spanSegmentCutInfo.OldSpanSegmentId} in span equipment: {spanEquipment.Id} ");

                if (!spanEquipment.TryGetSpanSegment(spanSegmentCutInfo.NewSpanSegmentId2, out var segment2withIndexInfo))
                    throw new ApplicationException($"Cannot find span segment with id: {spanSegmentCutInfo.OldSpanSegmentId} in span equipment: {spanEquipment.Id} ");

                // We need to go through all new span segments in the structure, and check if we need update the graph element due to segment index shift
                for (int newSegmentIndex = 0; newSegmentIndex < spanEquipment.SpanStructures[segment1withIndexInfo.StructureIndex].SpanSegments.Length; newSegmentIndex++)
                {
                    var newSegment = spanEquipment.SpanStructures[segment1withIndexInfo.StructureIndex].SpanSegments[newSegmentIndex];

                    if (TryGetGraphElement<IUtilityGraphSegmentRef>(newSegment.Id, out var existingUtilityGraphSegmentRef))
                    {
                        if (existingUtilityGraphSegmentRef.SegmentIndex != newSegmentIndex)
                        {
                            var newUtilityGraphElement = existingUtilityGraphSegmentRef.CreateWithNewSegmentIndex((ushort)newSegmentIndex);

                            if (newUtilityGraphElement is UtilityGraphConnectedSegment)
                                trans.Update((UtilityGraphConnectedSegment)newUtilityGraphElement);

                            UpdateDict(newSegment.Id, newUtilityGraphElement);
                        }
                    }
                }

                // If existing segment is disconnected, it's not in the graph either
                if (oldSegmentGraphElement is UtilityGraphDisconnectedSegment)
                {
                    AddDisconnectedSegment(spanEquipment, segment1withIndexInfo.StructureIndex, segment1withIndexInfo.SegmentIndex);
                    AddDisconnectedSegment(spanEquipment, segment2withIndexInfo.StructureIndex, segment2withIndexInfo.SegmentIndex);
                    RemoveFromDict(spanSegmentCutInfo.OldSpanSegmentId);
                }
                else
                {
                    var oldConnectedGraphElement = (UtilityGraphConnectedSegment)oldSegmentGraphElement;

                    // Create first/left segment
                    if (oldConnectedGraphElement.InV(version) != null)
                    {
                        var newSegment1GraphElement = new UtilityGraphConnectedSegment(spanSegmentCutInfo.NewSpanSegmentId1, (UtilityGraphConnectedTerminal)oldConnectedGraphElement.InV(version), null, spanEquipment.Id, segment1withIndexInfo.StructureIndex, segment1withIndexInfo.SegmentIndex);
                        trans.Add(newSegment1GraphElement);
                        AddToDict(newSegment1GraphElement.Id, newSegment1GraphElement);
                    }
                    // If InV is null, then we end with a disconnected segment
                    else
                    {
                        var newSegment1DisconnectedGraphElement = new UtilityGraphDisconnectedSegment(spanEquipment.Id, segment1withIndexInfo.StructureIndex, segment1withIndexInfo.SegmentIndex);
                        AddToDict(spanSegmentCutInfo.NewSpanSegmentId1, newSegment1DisconnectedGraphElement);
                    }



                    // Create second/right segment
                    if (oldConnectedGraphElement.OutV(version) != null)
                    {
                        var newSegment2GraphElement = new UtilityGraphConnectedSegment(spanSegmentCutInfo.NewSpanSegmentId2, null, (UtilityGraphConnectedTerminal)oldConnectedGraphElement.OutV(version), spanEquipment.Id, segment2withIndexInfo.StructureIndex, segment2withIndexInfo.SegmentIndex);
                        trans.Add(newSegment2GraphElement);
                        AddToDict(newSegment2GraphElement.Id, newSegment2GraphElement);
                    }
                    // If OutV is null, then we end with a disconnected segment
                    else
                    {
                        var newSegment2DisconnectedGraphElement = new UtilityGraphDisconnectedSegment(spanEquipment.Id, segment2withIndexInfo.StructureIndex, segment2withIndexInfo.SegmentIndex);
                        AddToDict(spanSegmentCutInfo.NewSpanSegmentId2, newSegment2DisconnectedGraphElement);
                    }

                    // Remove old segment
                    trans.Delete(oldConnectedGraphElement.Id);
                    RemoveFromDict(oldConnectedGraphElement.Id);
                }
            }
            finally
            {
                trans.Commit();
            }
        }

        internal void ApplySegmentConnect(SpanEquipment spanEquipment, SpanSegmentToSimpleTerminalConnectInfo spanSegmentToConnect)
        {
            if (!_graphElementsById.TryGetValue(spanSegmentToConnect.SegmentId, out var existingSegmentGraphElement))
                throw new ApplicationException($"Cannot find span segment graph element with id: {spanSegmentToConnect.SegmentId} processing segment to terminal connection.");

            if (!spanEquipment.TryGetSpanSegment(spanSegmentToConnect.SegmentId, out var spanSegmentWithIndexInfo))
                throw new ApplicationException($"Cannot find span segment with id: {spanSegmentToConnect.SegmentId} in span equipment with id: {spanEquipment.Id} ");

            var version = _objectManager.GetLatestCommitedVersion();

            var trans = _objectManager.CreateTransaction();

            try
            {
                // Find or create terminal
                var terminalToConnect = _objectManager.GetObject(spanSegmentToConnect.TerminalId, version) as UtilityGraphConnectedTerminal;

                if (terminalToConnect == null)
                {
                    var terminalNodeOfInterestId = spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal ? spanEquipment.NodesOfInterestIds[spanSegmentWithIndexInfo.SpanSegment.ToNodeOfInterestIndex] : spanEquipment.NodesOfInterestIds[spanSegmentWithIndexInfo.SpanSegment.FromNodeOfInterestIndex];
                    terminalToConnect = new UtilityGraphConnectedTerminal(spanSegmentToConnect.TerminalId, terminalNodeOfInterestId);
                    trans.Add(terminalToConnect);
                }

                // If segment has never been connected before
                if (existingSegmentGraphElement is UtilityGraphDisconnectedSegment)
                {
                    var fromNodeToConnect = spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromTerminalToSpanSegment ? terminalToConnect : null;
                    var toNodeToConnect = spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal ? terminalToConnect : null;

                    var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentToConnect.SegmentId, fromNodeToConnect, toNodeToConnect, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
              
                    trans.Add(newSegmentGraphElement);

                    UpdateDict(spanSegmentToConnect.SegmentId, newSegmentGraphElement);
                }
                // We're dealing with update to a segment already connected
                else
                {
                    if (spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal)
                    {
                        var existingFromTerminal = (UtilityGraphConnectedTerminal)((UtilityGraphConnectedSegment)existingSegmentGraphElement).InV(version);

                        var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentToConnect.SegmentId, existingFromTerminal, terminalToConnect, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
                        trans.Update(newSegmentGraphElement);

                        UpdateDict(spanSegmentToConnect.SegmentId, newSegmentGraphElement);
                    }
                    else
                    {
                        var existingToTerminal = (UtilityGraphConnectedTerminal)((UtilityGraphConnectedSegment)existingSegmentGraphElement).OutV(version);

                        var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentToConnect.SegmentId, terminalToConnect, existingToTerminal, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
                        trans.Update(newSegmentGraphElement);

                        UpdateDict(spanSegmentToConnect.SegmentId, newSegmentGraphElement);
                    }
                }
            }
            finally
            {
                trans.Commit();
            }
        }

        internal void ApplySegmentDisconnect(SpanEquipment spanEquipment, Guid spanSegmentId, Guid terminalId)
        {
            if (!TryGetGraphElement<UtilityGraphConnectedSegment>(spanSegmentId, out var existingSegmentGraphElement))
                throw new ApplicationException($"Cannot find connected span segment graph element with id: {spanSegmentId} processing segment to terminal disconnect.");

            if (!spanEquipment.TryGetSpanSegment(spanSegmentId, out var spanSegmentWithIndexInfo))
                throw new ApplicationException($"Cannot find span segment with id: {spanSegmentId} in span equipment with id: {spanEquipment.Id} ");

            var version = _objectManager.GetLatestCommitedVersion();

            if (existingSegmentGraphElement.InV(version)?.Id == terminalId)
            {
                var trans = _objectManager.CreateTransaction();

                var existingToTerminal = (UtilityGraphConnectedTerminal)existingSegmentGraphElement.OutV(version);
                var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentId, null, existingToTerminal, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
                trans.Update(newSegmentGraphElement);
                trans.Commit();

                UpdateDict(spanSegmentId, newSegmentGraphElement);
            }
            else if (existingSegmentGraphElement.OutV(version)?.Id == terminalId)
            {
                var trans = _objectManager.CreateTransaction();

                var existingFromTerminal = (UtilityGraphConnectedTerminal)existingSegmentGraphElement.InV(version);
                var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentId, existingFromTerminal, null, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
                trans.Update(newSegmentGraphElement);

                trans.Commit();
                UpdateDict(spanSegmentId, newSegmentGraphElement);
            }
            else
                throw new ApplicationException($"Cannot find any connection to terminal with id: {terminalId} in span segment with id: {spanSegmentId} in span equipment with id: {spanEquipment.Id}");
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
                        if (lastUpstreamSegment.InV(version) == null)
                        {
                            upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.NewGuid(), lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).FromNodeOfInterestIndex]));
                        }
                        else if (lastUpstreamSegment.OutV(version) == null)
                        {
                            upstreamTrace.Add(new UtilityGraphConnectedTerminal(Guid.NewGuid(), lastUpstreamSegment.SpanEquipment(_utilityNetworkProjection).NodesOfInterestIds[lastUpstreamSegment.SpanSegment(_utilityNetworkProjection).ToNodeOfInterestIndex]));
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

        private void UpdateDict(Guid segmentId, IUtilityGraphSegmentRef newUtilityGraphSegmentRef)
        {
            RemoveFromDict(segmentId);
            AddToDict(segmentId, newUtilityGraphSegmentRef);
        }

        private void RemoveFromDict(Guid segmentId)
        {
            if (!_graphElementsById.TryRemove(segmentId, out _))
                throw new ApplicationException($"Cannot remove segment with id: {segmentId} from graph.");
        }

        private void AddToDict(Guid segmentId, IUtilityGraphSegmentRef newUtilityGraphSegmentRef)
        {
            if (!_graphElementsById.TryAdd(segmentId, newUtilityGraphSegmentRef))
                throw new ArgumentException($"A span segment with id: {segmentId} already exists in the graph.");
        }

    }
}
