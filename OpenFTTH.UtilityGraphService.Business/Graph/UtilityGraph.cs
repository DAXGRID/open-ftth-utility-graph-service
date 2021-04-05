﻿using FluentResults;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Trace;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Concurrent;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraph
    {
        ConcurrentDictionary<Guid, IUtilityGraphElement> _graphElementsById = new ConcurrentDictionary<Guid, IUtilityGraphElement>();

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
            if (!_graphElementsById.TryRemove(spanSegmentCutInfo.OldSpanSegmentId, out _))
                throw new ApplicationException($"Cannot remove span segment with id: {spanSegmentCutInfo.OldSpanSegmentId} from graph.");

            if (!spanEquipment.TryGetSpanSegment(spanSegmentCutInfo.NewSpanSegmentId1, out var segment1withIndexInfo))
                throw new ApplicationException($"Cannot find span segment with id: {spanSegmentCutInfo.OldSpanSegmentId} in span equipment: {spanEquipment.Id} ");

            AddDisconnectedSegment(spanEquipment, segment1withIndexInfo.StructureIndex, segment1withIndexInfo.SegmentIndex);

            if (!spanEquipment.TryGetSpanSegment(spanSegmentCutInfo.NewSpanSegmentId2, out var segment2withIndexInfo))
                throw new ApplicationException($"Cannot find span segment with id: {spanSegmentCutInfo.OldSpanSegmentId} in span equipment: {spanEquipment.Id} ");

            AddDisconnectedSegment(spanEquipment, segment2withIndexInfo.StructureIndex, segment2withIndexInfo.SegmentIndex);

            // TODO: Update all segments in the structure (because index shifted)
            // TODO: Update graph connectivity
        }

        internal void ApplySegmentConnect(SpanEquipment spanEquipment, SpanSegmentToSimpleTerminalConnectInfo spanSegmentToConnect)
        {
            // TODO: Update graph connectivity
        }

        internal void ApplySegmentDisconnect(SpanEquipment spanEquipment, Guid spanSegmentId, Guid terminalId)
        {
            // TODO: Update graph connectivity
        }

        public SpanSegmentTraceResult TraceSegment(Guid id)
        {
            var result = new SpanSegmentTraceResult() { SpanSegmentId = id };

            if (_graphElementsById.TryGetValue(id, out var utilityGraphElement))
            {
                if (utilityGraphElement is UtilityGraphDisconnectedSegment)
                {
                    var connectedSegment = utilityGraphElement as UtilityGraphDisconnectedSegment;

                    

                }
            }

            return result;
        }
    }
}
