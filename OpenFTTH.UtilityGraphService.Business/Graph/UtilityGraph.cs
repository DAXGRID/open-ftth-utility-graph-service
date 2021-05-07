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
        private ConcurrentDictionary<Guid, IUtilityGraphElement> _graphElementsById = new ConcurrentDictionary<Guid, IUtilityGraphElement>();

        private InMemoryObjectManager _objectManager = new InMemoryObjectManager();

        public UtilityGraph()
        {
            
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

                    // Create the two loneny terminals in the node between the segments (becase we are cut)
                    var terminalBetweenSegmentsNodeOfInterestId = spanEquipment.NodesOfInterestIds[segment1withIndexInfo.SpanSegment.ToNodeOfInterestIndex];

                    if (terminalBetweenSegmentsNodeOfInterestId == Guid.Parse("020c3eb8-13cc-4c0e-a331-c7610f996a52") && segment1withIndexInfo.StructureIndex == 1)
                    {

                    }

                    var terminal1BetweenSegments = new UtilityGraphConnectedTerminal(Guid.NewGuid(), terminalBetweenSegmentsNodeOfInterestId);
                    trans.Add(terminal1BetweenSegments);

                    var terminal2BetweenSegments = new UtilityGraphConnectedTerminal(Guid.NewGuid(), terminalBetweenSegmentsNodeOfInterestId);
                    trans.Add(terminal2BetweenSegments);

                    // Create first/left segment
                    var newSegment1GraphElement = new UtilityGraphConnectedSegment(spanSegmentCutInfo.NewSpanSegmentId1, (UtilityGraphConnectedTerminal)oldConnectedGraphElement.InV(version), terminal1BetweenSegments, spanEquipment.Id, segment1withIndexInfo.StructureIndex, segment1withIndexInfo.SegmentIndex);
                    trans.Add(newSegment1GraphElement);

                    AddToDict(newSegment1GraphElement.Id, newSegment1GraphElement);

                    // Create second/right segment
                    var newSegment2GraphElement = new UtilityGraphConnectedSegment(spanSegmentCutInfo.NewSpanSegmentId2, terminal2BetweenSegments, (UtilityGraphConnectedTerminal)oldConnectedGraphElement.OutV(version), spanEquipment.Id, segment2withIndexInfo.StructureIndex, segment2withIndexInfo.SegmentIndex);
                    trans.Add(newSegment2GraphElement);

                    AddToDict(newSegment2GraphElement.Id, newSegment2GraphElement);

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
                    var dummyTerminalNodeOfInterestId = spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal ? spanEquipment.NodesOfInterestIds[spanSegmentWithIndexInfo.SpanSegment.FromNodeOfInterestIndex] : spanEquipment.NodesOfInterestIds[spanSegmentWithIndexInfo.SpanSegment.ToNodeOfInterestIndex];
                    var dummyTerminal = new UtilityGraphConnectedTerminal(Guid.NewGuid(), dummyTerminalNodeOfInterestId);
                    var fromNodeToConnect = spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromTerminalToSpanSegment ? terminalToConnect : dummyTerminal;
                    var toNodeToConnect = spanSegmentToConnect.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal ? terminalToConnect : dummyTerminal;

                    var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentToConnect.SegmentId, fromNodeToConnect, toNodeToConnect, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);

                    // Add segment and terminals
                    trans.Add(dummyTerminal);
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

            if (existingSegmentGraphElement.InV(version).Id == terminalId)
            {
                var trans = _objectManager.CreateTransaction();

                var dummyTerminalNodeOfInterestId = spanEquipment.NodesOfInterestIds[spanSegmentWithIndexInfo.SpanSegment.FromNodeOfInterestIndex];

                var dummyTerminal = new UtilityGraphConnectedTerminal(Guid.NewGuid(), dummyTerminalNodeOfInterestId);
                trans.Add(dummyTerminal);

                var existingToTerminal = (UtilityGraphConnectedTerminal)((UtilityGraphConnectedSegment)existingSegmentGraphElement).OutV(version);
                var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentId, dummyTerminal, existingToTerminal, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
                trans.Update(newSegmentGraphElement);
                trans.Commit();

                UpdateDict(spanSegmentId, newSegmentGraphElement);
            }
            else if (existingSegmentGraphElement.OutV(version).Id == terminalId)
            {
                var trans = _objectManager.CreateTransaction();

                var dummyTerminalNodeOfInterestId = spanEquipment.NodesOfInterestIds[spanSegmentWithIndexInfo.SpanSegment.ToNodeOfInterestIndex];
 
                var dummyTerminal = new UtilityGraphConnectedTerminal(Guid.NewGuid(), dummyTerminalNodeOfInterestId);
                trans.Add(dummyTerminal);

                var existingFromTerminal = (UtilityGraphConnectedTerminal)((UtilityGraphConnectedSegment)existingSegmentGraphElement).InV(version);
                var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentId, existingFromTerminal, dummyTerminal, spanEquipment.Id, spanSegmentWithIndexInfo.StructureIndex, spanSegmentWithIndexInfo.SegmentIndex);
                trans.Update(newSegmentGraphElement);

                trans.Commit();
                UpdateDict(spanSegmentId, newSegmentGraphElement);
            }
            else
                throw new ApplicationException($"Cannot find any connection to terminal with id: {terminalId} in span segment with id: {spanSegmentId} in span equipment with id: {spanEquipment.Id}");
        }

        internal void ApplyChangeSegmentFromTerminalToNewNodeOfInterest(Guid spanSegmentId, Guid newNodeOfInterestId)
        {
            if (!TryGetGraphElement<UtilityGraphConnectedSegment>(spanSegmentId, out var existingSegmentGraphElement))
                throw new ApplicationException($"Cannot find connected span segment graph element with id: {spanSegmentId} processing segment to terminal disconnect.");

            var version = _objectManager.GetLatestCommitedVersion();

            var trans = _objectManager.CreateTransaction();

            try
            {
                var dummyTerminal = new UtilityGraphConnectedTerminal(Guid.NewGuid(), newNodeOfInterestId);
                trans.Add(dummyTerminal);

                var existingToTerminal = (UtilityGraphConnectedTerminal)((UtilityGraphConnectedSegment)existingSegmentGraphElement).OutV(version);
                var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentId, dummyTerminal, existingToTerminal, existingSegmentGraphElement.SpanEquipmentId, existingSegmentGraphElement.StructureIndex, existingSegmentGraphElement.SegmentIndex);
                trans.Update(newSegmentGraphElement);

                UpdateDict(spanSegmentId, newSegmentGraphElement);
            }
            finally
            {
                trans.Commit();
            }
        }

        internal void ApplyChangeSegmentToTerminalToNewNodeOfInterest(Guid spanSegmentId, Guid newNodeOfInterestId)
        {
            if (!TryGetGraphElement<UtilityGraphConnectedSegment>(spanSegmentId, out var existingSegmentGraphElement))
                throw new ApplicationException($"Cannot find connected span segment graph element with id: {spanSegmentId} processing segment to terminal disconnect.");

            var version = _objectManager.GetLatestCommitedVersion();

            var trans = _objectManager.CreateTransaction();

            try
            {
                var dummyTerminal = new UtilityGraphConnectedTerminal(Guid.NewGuid(), newNodeOfInterestId);
                trans.Add(dummyTerminal);

                var existingFromTerminal = (UtilityGraphConnectedTerminal)((UtilityGraphConnectedSegment)existingSegmentGraphElement).InV(version);
                var newSegmentGraphElement = new UtilityGraphConnectedSegment(spanSegmentId, existingFromTerminal, dummyTerminal, existingSegmentGraphElement.SpanEquipmentId, existingSegmentGraphElement.StructureIndex, existingSegmentGraphElement.SegmentIndex);
                trans.Update(newSegmentGraphElement);

                UpdateDict(spanSegmentId, newSegmentGraphElement);
            }
            finally
            {
                trans.Commit();
            }
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

                    var upstreamTrace = connectedSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                        version,
                        n => n != connectedSegment.InV(version)
                    );

                    result.Upstream = upstreamTrace.ToArray();

                    var downstreamTrace = connectedSegment.UndirectionalDFS<UtilityGraphConnectedTerminal, UtilityGraphConnectedSegment>(
                        version,
                        n => n != connectedSegment.OutV(version)
                    );

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
