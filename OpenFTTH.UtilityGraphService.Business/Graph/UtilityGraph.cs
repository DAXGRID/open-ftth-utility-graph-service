using FluentResults;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
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

        public void AddDisconnectedSegment(SpanEquipment spanEquipment, UInt16 structureIndex)
        {
            var spanSegment = spanEquipment.SpanStructures[structureIndex].SpanSegments[0];

            var disconnectedGraphSegment = new UtilityGraphDisconnectedSegment(spanEquipment, structureIndex);

            if (!_graphElementsById.TryAdd(spanSegment.Id, disconnectedGraphSegment))
                throw new ArgumentException($"A span segment with id: {spanSegment.Id} already exists in the graph.");
        }
    }
}
