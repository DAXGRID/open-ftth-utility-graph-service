using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Projections
{
    /// <summary>
    /// Functions that apply events to a span equipment immutable object and return a new copy
    /// </summary>
    public static class SpanEquipmentProjectionFunctions
    {
        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanSegmentsCut spanSegmentsCutEvent)
        {
            // Cut them segments
            List<SpanStructure> newStructures = new List<SpanStructure>();

            // Create dictionary of cuts for fast lookup
            Dictionary<Guid, SpanSegmentCutInfo> spanSegmentCutInfoBySegmentId = spanSegmentsCutEvent.Cuts.ToDictionary(c => c.OldSpanSegmentId);

            // First create a new nodes of interest list with the cut node added
            Guid[] newNodeOfInterestIdList = CreateNewNodeOfInterestIdListWith(existingSpanEquipment, spanSegmentsCutEvent.CutNodeOfInterestId, spanSegmentsCutEvent.CutNodeOfInterestIndex);

            bool nodeOfInterestAlreadyExists = existingSpanEquipment.NodesOfInterestIds.Contains(spanSegmentsCutEvent.CutNodeOfInterestId);

            // Loop though all span structures
            for (UInt16 structureIndex = 0; structureIndex < existingSpanEquipment.SpanStructures.Length; structureIndex++)
            {
                var existingSpanStructure = existingSpanEquipment.SpanStructures[structureIndex];

                List<SpanSegment> newSegments = new List<SpanSegment>();

                // Loop throughh all span segments
                foreach (var existingSegment in existingSpanStructure.SpanSegments)
                {
                    UInt16 fromNodeOfInterestIndexToUse = existingSegment.FromNodeOfInterestIndex;
                    UInt16 toNodeOfInterestIndexToUse = existingSegment.ToNodeOfInterestIndex;

                    if (!nodeOfInterestAlreadyExists)
                    {
                        if (fromNodeOfInterestIndexToUse >= spanSegmentsCutEvent.CutNodeOfInterestIndex)
                            fromNodeOfInterestIndexToUse++;

                        if (toNodeOfInterestIndexToUse >= spanSegmentsCutEvent.CutNodeOfInterestIndex)
                            toNodeOfInterestIndexToUse++;
                    }

                    // If cut info exists
                    if (spanSegmentCutInfoBySegmentId.TryGetValue(existingSegment.Id, out var spanSegmentCutInfo))
                    {
                        // Add the first segment
                        newSegments.Add(
                            new SpanSegment(
                                id: spanSegmentCutInfo.NewSpanSegmentId1,
                                fromNodeOfInterestIndex: fromNodeOfInterestIndexToUse,
                                toNodeOfInterestIndex: spanSegmentsCutEvent.CutNodeOfInterestIndex)
                            );

                        // Add the second segment
                        newSegments.Add(
                            new SpanSegment(
                                id: spanSegmentCutInfo.NewSpanSegmentId2,
                                fromNodeOfInterestIndex: spanSegmentsCutEvent.CutNodeOfInterestIndex,
                                toNodeOfInterestIndex: toNodeOfInterestIndexToUse)
                            );
                    }
                    // If no cut info exists
                    else
                    {
                        if (!nodeOfInterestAlreadyExists)
                        {
                            var newSegment = existingSegment with { FromNodeOfInterestIndex = fromNodeOfInterestIndexToUse, ToNodeOfInterestIndex = toNodeOfInterestIndexToUse };
                            newSegments.Add(newSegment);
                        }
                        else
                        {
                            newSegments.Add(existingSegment);
                        }
                    }
                }

                newStructures.Add(
                    existingSpanStructure with
                    {
                        SpanSegments = newSegments.ToArray()
                    });
            }

            return existingSpanEquipment with
            {
                NodesOfInterestIds = newNodeOfInterestIdList,
                SpanStructures = newStructures.ToArray()
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanEquipmentAffixedToContainer spanEquipmentAffixedToContainer)
        {
            var newListOfAffixes = new List<SpanEquipmentNodeContainerAffix>();

            if (existingSpanEquipment.NodeContainerAffixes != null)
            {
                foreach (var existingAffix in existingSpanEquipment.NodeContainerAffixes)
                    newListOfAffixes.Add(existingAffix);
            }

            newListOfAffixes.Add(spanEquipmentAffixedToContainer.Affix);

            return existingSpanEquipment with
            {
                NodeContainerAffixes = newListOfAffixes.ToArray()
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanEquipmentDetachedFromContainer @event)
        {
            var newListOfAffixes = new List<SpanEquipmentNodeContainerAffix>();

            if (existingSpanEquipment.NodeContainerAffixes != null)
            {
                foreach (var existingAffix in existingSpanEquipment.NodeContainerAffixes)
                {
                    if (existingAffix.NodeContainerId != @event.NodeContainerId)
                        newListOfAffixes.Add(existingAffix);
                }
            }
            
            return existingSpanEquipment with
            {
                NodeContainerAffixes = newListOfAffixes.ToArray()
            };
        }

        private static Guid[] CreateNewNodeOfInterestIdListWith(SpanEquipment existingSpanEquipment, Guid cutNodeOfInterestId, UInt16 newNodeOfInterestIndex)
        {
            if (existingSpanEquipment.NodesOfInterestIds.Contains(cutNodeOfInterestId))
                return existingSpanEquipment.NodesOfInterestIds;

            var result = new List<Guid>();

            for (UInt16 i = 0; i < newNodeOfInterestIndex; i++)
            {
                result.Add(existingSpanEquipment.NodesOfInterestIds[i]);
            }

            result.Add(cutNodeOfInterestId);

            for (UInt16 i = newNodeOfInterestIndex; i < existingSpanEquipment.NodesOfInterestIds.Length; i++)
            {
                result.Add(existingSpanEquipment.NodesOfInterestIds[i]);
            }

            return result.ToArray();
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanSegmentsConnectedToSimpleTerminals @event)
        {
            List<SpanStructure> newStructures = new List<SpanStructure>();

            // Create dictionary of cuts for fast lookup
            Dictionary<Guid, SpanSegmentToSimpleTerminalConnectInfo> spanSegmentConnectInfoBySegmentId = @event.Connects.ToDictionary(c => c.SegmentId);

            // Loop though all span structures
            for (UInt16 structureIndex = 0; structureIndex < existingSpanEquipment.SpanStructures.Length; structureIndex++)
            {
                var existingSpanStructure = existingSpanEquipment.SpanStructures[structureIndex];

                List<SpanSegment> newSegments = new List<SpanSegment>();

                // Loop through all span segments
                foreach (var existingSegment in existingSpanStructure.SpanSegments)
                {
                    // If connect info exists
                    if (spanSegmentConnectInfoBySegmentId.TryGetValue(existingSegment.Id, out var spanSegmentConnectInfo))
                    {
                        if (spanSegmentConnectInfo.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal)
                        {
                            newSegments.Add(
                                existingSegment with { ToTerminalId = spanSegmentConnectInfo.TerminalId }
                            );
                        }
                        else
                        {
                            newSegments.Add(
                                existingSegment with { FromTerminalId = spanSegmentConnectInfo.TerminalId }
                            );
                        }
                    }
                    else
                    {
                        newSegments.Add(existingSegment);
                    }
                }

                newStructures.Add(
                    existingSpanStructure with
                    {
                        SpanSegments = newSegments.ToArray()
                    }
                );
            }

            return existingSpanEquipment with
            {
                SpanStructures = newStructures.ToArray()
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanSegmentDisconnectedFromTerminal @event)
        {
            List<SpanStructure> newStructures = new List<SpanStructure>();

            // Loop though all span structures
            for (UInt16 structureIndex = 0; structureIndex < existingSpanEquipment.SpanStructures.Length; structureIndex++)
            {
                var existingSpanStructure = existingSpanEquipment.SpanStructures[structureIndex];

                List<SpanSegment> newSegments = new List<SpanSegment>();

                // Loop through all span segments
                foreach (var existingSegment in existingSpanStructure.SpanSegments)
                {
                    // If disconnect
                    if (existingSegment.Id == @event.SpanSegmentId)
                    {
                        if (existingSegment.FromTerminalId == @event.TerminalId)
                        {
                            newSegments.Add(
                                existingSegment with { FromTerminalId = Guid.Empty }
                            );
                        }
                        else if(existingSegment.ToTerminalId == @event.TerminalId)
                        {
                            newSegments.Add(
                                existingSegment with { ToTerminalId = Guid.Empty }
                            );
                        }
                    }
                    else
                    {
                        newSegments.Add(existingSegment);
                    }
                }

                newStructures.Add(
                    existingSpanStructure with
                    {
                        SpanSegments = newSegments.ToArray()
                    }
                );
            }

            return existingSpanEquipment with
            {
                SpanStructures = newStructures.ToArray()
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, AdditionalStructuresAddedToSpanEquipment @event)
        {
            List<SpanStructure> newStructures = new List<SpanStructure>();

            // Copy all span structures
            foreach (var existingStructure in existingSpanEquipment.SpanStructures)
            {
                newStructures.Add(existingStructure);
            }

            // Add new ones
            newStructures.AddRange(@event.SpanStructuresToAdd);

            return existingSpanEquipment with
            {
                SpanStructures = newStructures.ToArray()
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanStructureRemoved @event)
        {
            List<SpanStructure> newStructures = new();

            // Copy all span structures
            foreach (var existingStructure in existingSpanEquipment.SpanStructures)
            {
                if (existingStructure.Id == @event.SpanStructureId)
                {
                    newStructures.Add(existingStructure with { Deleted = true });
                }
                else
                {
                    newStructures.Add(existingStructure);
                }
            }

            return existingSpanEquipment with
            {
                SpanStructures = newStructures.ToArray()
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanEquipmentMoved @event)
        {
            return existingSpanEquipment with
            {
                NodesOfInterestIds = @event.NodesOfInterestIds
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanEquipmentMerged @event)
        {
            return existingSpanEquipment with
            {
                NodesOfInterestIds = @event.NodesOfInterestIds
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanEquipmentMarkingInfoChanged @event)
        {
            return existingSpanEquipment with
            {
                MarkingInfo = @event.MarkingInfo
            };
        }

        public static SpanEquipment Apply(SpanEquipment existingSpanEquipment, SpanEquipmentManufacturerChanged @event)
        {
            return existingSpanEquipment with
            {
                ManufacturerId = @event.ManufacturerId
            };
        }

    }
}
