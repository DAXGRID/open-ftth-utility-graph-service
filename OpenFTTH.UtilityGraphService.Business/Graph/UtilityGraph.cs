using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraph
    {
        ConcurrentDictionary<Guid, IUtilityGraphElement> _graphElementsById = new ConcurrentDictionary<Guid, IUtilityGraphElement>();

        public UtilityGraph()
        {
            
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
