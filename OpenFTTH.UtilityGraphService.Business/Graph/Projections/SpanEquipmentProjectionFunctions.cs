using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Projections
{
    /// <summary>
    /// Functions that apply events to a span equipment immutable object and return a copy of the new state
    /// </summary>
    public static class SpanEquipmentProjectionFunctions
    {
        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanSegmentsCut spanSegmentsCutEvent)
        {
            // First create a new nodes of interest list with the cut node added
            List<Guid> newNodeOfInterestIdList = CreateNodeOfInterestIdListWith(existingSpanEquipment, spanSegmentsCutEvent.CutNodeOfInterestId);
            UInt16 cutNodeOfInterestIndex = (UInt16)(newNodeOfInterestIdList.Count - 1);

            // Cut them segments
            List<SpanStructure> newStructures = new List<SpanStructure>();

            // Create dictionary of cuts for fast lookup
            Dictionary<UInt16, SpanSegmentCutInfo> spanSegmentCutInfoByStructureIndex = spanSegmentsCutEvent.Cuts.ToDictionary(c => c.OldSegmentIndex);

            // Go though all span structures
            for (UInt16 structureIndex = 0; structureIndex < existingSpanEquipment.SpanStructures.Length; structureIndex++)
            {
                // If cut info exists
                if (spanSegmentCutInfoByStructureIndex.TryGetValue(structureIndex, out var spanSegmentCutInfo))
                {
                    List<SpanSegment> newSegments = new List<SpanSegment>();

                    var existingSpanStructure = existingSpanEquipment.SpanStructures[structureIndex];

                    foreach (var existingSegment in existingSpanStructure.SpanSegments)
                    {
                        if (existingSegment.Id == spanSegmentCutInfo.OldSpanSegmentId)
                        {
                            // Add the first segment
                            newSegments.Add(
                                new SpanSegment(
                                    id: spanSegmentCutInfo.NewSpanSegmentId1,
                                    fromNodeOfInterestIndex: existingSegment.FromNodeOfInterestIndex,
                                    toNodeOfInterestIndex: cutNodeOfInterestIndex)
                                );

                            // Add the second segment
                            newSegments.Add(
                                new SpanSegment(
                                    id: spanSegmentCutInfo.NewSpanSegmentId2,
                                    fromNodeOfInterestIndex: cutNodeOfInterestIndex,
                                    toNodeOfInterestIndex: existingSegment.ToNodeOfInterestIndex)
                                );
                        }
                        else
                        {
                            newSegments.Add(existingSegment);
                        }
                    }

                    newStructures.Add(
                        existingSpanStructure with { 
                            SpanSegments = ImmutableArray.Create(newSegments.ToArray())
                        });
                }
                // If no cut info exists
                else
                {
                    // We just add the existing structure as-is
                    newStructures.Add(existingSpanEquipment.SpanStructures[structureIndex]);
                }
            }

            return existingSpanEquipment with {
                NodesOfInterestIds = newNodeOfInterestIdList.ToArray(),
                SpanStructures = ImmutableArray.Create(newStructures.ToArray())
            };
        }

        private static List<Guid> CreateNodeOfInterestIdListWith(SpanEquipment existingSpanEquipment, Guid cutNodeOfInterestId)
        {
            return new List<Guid>(existingSpanEquipment.NodesOfInterestIds)
            {
                cutNodeOfInterestId
            };
        }
    }
}
