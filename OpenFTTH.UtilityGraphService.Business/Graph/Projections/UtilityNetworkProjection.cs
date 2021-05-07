using DAX.EventProcessing;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Projections;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityNetworkProjection : ProjectionBase
    {
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByEquipmentId = new();
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByInterestId = new();
        private readonly ConcurrentDictionary<Guid, NodeContainer> _nodeContainerByEquipmentId = new();
        private readonly ConcurrentDictionary<Guid, NodeContainer> _nodeContainerByInterestId = new();

        private readonly UtilityGraph _utilityGraph = new();

        public UtilityGraph Graph => _utilityGraph;

        public LookupCollection<NodeContainer> NodeContainers => new LookupCollection<NodeContainer>(_nodeContainerByEquipmentId.Values);

        public LookupCollection<SpanEquipment> SpanEquipments => new LookupCollection<SpanEquipment>(_spanEquipmentByEquipmentId.Values);
        
        public UtilityNetworkProjection(IExternalEventProducer externalEventProducer)
        {
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
            ProjectEvent<SpanEquipmentAffixedToContainer>(Project);
            ProjectEvent<SpanEquipmentDetachedFromContainer>(Project);
            ProjectEvent<SpanSegmentsCut>(Project);
            ProjectEvent<SpanSegmentsConnectedToSimpleTerminals>(Project);
            ProjectEvent<SpanSegmentDisconnectedFromTerminal>(Project);
            ProjectEvent<AdditionalStructuresAddedToSpanEquipment>(Project);
            ProjectEvent<SpanStructureRemoved>(Project);
            ProjectEvent<SpanEquipmentRemoved>(Project);
            ProjectEvent<SpanEquipmentMoved>(Project);
            ProjectEvent<SpanEquipmentMerged>(Project);
            ProjectEvent<SpanEquipmentMarkingInfoChanged>(Project);
            ProjectEvent<SpanEquipmentManufacturerChanged>(Project);
            ProjectEvent<SpanEquipmentSpecificationChanged>(Project);


            ProjectEvent<NodeContainerPlacedInRouteNetwork>(Project);
            ProjectEvent<NodeContainerVerticalAlignmentReversed>(Project);
        }

        public bool TryGetEquipment<T>(Guid equipmentOrInterestId, out T equipment) where T: IEquipment
        {
            if (_spanEquipmentByEquipmentId.TryGetValue(equipmentOrInterestId, out SpanEquipment? spanEquipmentByEquipmentId))
            {
                if (spanEquipmentByEquipmentId is T)
                {
                    equipment = (T)(object)spanEquipmentByEquipmentId;
                    return true;
                }
            }
            else if (_spanEquipmentByInterestId.TryGetValue(equipmentOrInterestId, out SpanEquipment? spanEquipmentByInterestId))
            {
                if (spanEquipmentByInterestId is T)
                {
                    equipment = (T)(object)spanEquipmentByInterestId;
                    return true;
                }
            }
            else if (_nodeContainerByEquipmentId.TryGetValue(equipmentOrInterestId, out NodeContainer? nodeContainerByEquipmentId))
            {
                if (nodeContainerByEquipmentId is T)
                {
                    equipment = (T)(object)nodeContainerByEquipmentId;
                    return true;
                }
            }
            else if (_nodeContainerByInterestId.TryGetValue(equipmentOrInterestId, out NodeContainer? nodeContainerByInterestId))
            {
                if (nodeContainerByInterestId is T)
                {
                    equipment = (T)(object)nodeContainerByInterestId;
                    return true;
                }
            }
            else if (_utilityGraph.TryGetGraphElement<IUtilityGraphSegmentRef>(equipmentOrInterestId, out var utilityGraphSegmentRef))
            {
                if (utilityGraphSegmentRef.SpanEquipment(this) is T)
                {
                    equipment = (T)(object)utilityGraphSegmentRef.SpanEquipment(this);
                    return true;
                }
            }

            #pragma warning disable CS8601 // Possible null reference assignment.
            equipment = default(T);
            #pragma warning restore CS8601 // Possible null reference assignment.

            return false;
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanEquipmentPlacedInRouteNetwork @event):
                    StoreAndIndexVirginSpanEquipment(@event.Equipment);
                    break;

                case (AdditionalStructuresAddedToSpanEquipment @event):
                    ProcessAdditionalStructures(@event);
                    break;

                case (SpanStructureRemoved @event):
                    ProcessInnerStructureRemoval(@event);
                    break;

                case (SpanEquipmentAffixedToContainer @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentDetachedFromContainer @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanSegmentsCut @event):
                    ProcesstSegmentCuts(@event);
                    break;

                case (SpanSegmentsConnectedToSimpleTerminals @event):
                    ProcessSegmentConnects(@event);
                    break;

                case (SpanSegmentDisconnectedFromTerminal @event):
                    ProcessSegmentDisconnects(@event);
                    break;

                case (SpanEquipmentRemoved @event):
                    ProcessSpanEquipmentRemoval(@event);
                    break;

                case (SpanEquipmentMoved @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentMerged @event):
                    ProcessSpanEquipmentMerge(@event);
                    break;

                case (SpanEquipmentMarkingInfoChanged @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentManufacturerChanged @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentSpecificationChanged @event):
                    ProcessSpanEquipmentSpecificationChange(@event);
                    break;

                case (NodeContainerPlacedInRouteNetwork @event):
                    StoreAndIndexVirginContainerEquipment(@event.Container);
                    break;

                case (NodeContainerVerticalAlignmentReversed @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;
            }
        }

        private void StoreAndIndexVirginSpanEquipment(SpanEquipment spanEquipment)
        {
            // Store the new span equipment in memory
            _spanEquipmentByEquipmentId.TryAdd(spanEquipment.Id, spanEquipment);
            _spanEquipmentByInterestId.TryAdd(spanEquipment.WalkOfInterestId, spanEquipment);

            // Add span segments to the graph
            for (UInt16 structureIndex = 0; structureIndex < spanEquipment.SpanStructures.Length; structureIndex++)
            {
                // We're dealing with a virgin span equipment and therefore only disconnected segments at index 0
                _utilityGraph.AddDisconnectedSegment(spanEquipment, structureIndex, 0);
            }
        }

        private void StoreAndIndexVirginContainerEquipment(NodeContainer nodeContainer)
        {
            // Store the new span equipment in memory
            _nodeContainerByEquipmentId.TryAdd(nodeContainer.Id, nodeContainer);
            _nodeContainerByInterestId.TryAdd(nodeContainer.InterestId, nodeContainer);
        }

        private void ProcesstSegmentCuts(SpanSegmentsCut @event)
        {
            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            if (spanEquipment.Id == Guid.Parse("49aef63a-1295-4094-a436-6d45f83a6210"))
            {

            }

            // Re-index segments cut
            foreach (var spanSegmentCut in @event.Cuts)
            {
                _utilityGraph.ApplySegmentCut(spanEquipment, spanSegmentCut);
            }
        }

        private void ProcessSegmentConnects(SpanSegmentsConnectedToSimpleTerminals @event)
        {
            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            // Re-index segments connect
            foreach (var spanSegmentToConnect in @event.Connects)
            {
                _utilityGraph.ApplySegmentConnect(spanEquipment, spanSegmentToConnect);
            }
        }

        private void ProcessSegmentDisconnects(SpanSegmentDisconnectedFromTerminal @event)
        {
            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            _utilityGraph.ApplySegmentDisconnect(spanEquipment, @event.SpanSegmentId, @event.TerminalId);
        }

        private void ProcessAdditionalStructures(AdditionalStructuresAddedToSpanEquipment @event)
        {
            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];


            // Add new span structures to the graph
            foreach (var spanStructure in @event.SpanStructuresToAdd)
            {
                // We're dealing with a virgin span structures and therefore only disconnected segments at index 0
                _utilityGraph.AddDisconnectedSegment(spanEquipment, spanStructure.Position, 0);
            }
        }

        private void ProcessInnerStructureRemoval(SpanStructureRemoved @event)
        {
            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            var removedInnerSpanStructure = spanEquipment.SpanStructures.First(s => s.Id == @event.SpanStructureId);

            // Remove span segments from the graph
            foreach (var spanSegment in removedInnerSpanStructure.SpanSegments)
            {
                _utilityGraph.RemoveDisconnectedSegment(spanSegment.Id);
            }
        }


        private void ProcessSpanEquipmentRemoval(SpanEquipmentRemoved @event)
        {
            var existingSpanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            TryRemove(@event.SpanEquipmentId, existingSpanEquipment.WalkOfInterestId);

            // Remove span segments from the graph
            foreach (var spanStructure in existingSpanEquipment.SpanStructures)
            {
                if (!spanStructure.Deleted)
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        _utilityGraph.RemoveDisconnectedSegment(spanSegment.Id);
                    }
                }
            }
        }

        private void ProcessSpanEquipmentSpecificationChange(SpanEquipmentSpecificationChanged @event)
        {
            var spanEquipmentBeforeChange = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipmentAfterChange = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            var structuresToBeDeleted = @event.StructureModificationInstructions.Where(i => i.StructureToBeDeleted).ToDictionary(i => i.StructureId);
            var structuresToBeAdded = @event.StructureModificationInstructions.Where(i => i.NewStructureToBeInserted != null).ToDictionary(i => i.StructureId);

            // Deleted span segments from the graph
            foreach (var spanStructure in spanEquipmentBeforeChange.SpanStructures)
            {
                if (structuresToBeDeleted.ContainsKey(spanStructure.Id))
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        _utilityGraph.RemoveDisconnectedSegment(spanSegment.Id);
                    }
                }
            }

            // Add structures
            foreach (var structureToBeAddedInstruction in @event.StructureModificationInstructions.Where(i => i.NewStructureToBeInserted != null))
            {
                if (structureToBeAddedInstruction.NewStructureToBeInserted != null)
                {
                    _utilityGraph.AddDisconnectedSegment(spanEquipmentAfterChange, structureToBeAddedInstruction.NewStructureToBeInserted.Position, 0);
                }
            }
        }

        private void ProcessSpanEquipmentMerge(SpanEquipmentMerged @event)
        {
            var spanEquipmentBeforeChange = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));

            var spanEquipmentAfterChange = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            // If from end is moved
            if (@event.NodesOfInterestIds.First() != spanEquipmentBeforeChange.NodesOfInterestIds.First())
            {
                foreach (var spanStructure in spanEquipmentBeforeChange.SpanStructures)
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        if (spanSegment.ToTerminalId != Guid.Empty)
                        {
                            _utilityGraph.ApplyChangeSegmentFromTerminalToNewNodeOfInterest(spanSegment.Id, @event.NodesOfInterestIds.First());
                        }
                    }
                }
            }

            // If to end is moved
            if (@event.NodesOfInterestIds.Last() != spanEquipmentBeforeChange.NodesOfInterestIds.Last())
            {
                foreach (var spanStructure in spanEquipmentBeforeChange.SpanStructures)
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        if (spanSegment.FromTerminalId != Guid.Empty)
                        {
                            _utilityGraph.ApplyChangeSegmentToTerminalToNewNodeOfInterest(spanSegment.Id, @event.NodesOfInterestIds.Last());
                        }
                    }
                }
            }
        }

        private void TryUpdate(SpanEquipment newSpanEquipmentState)
        {
            var oldSpanEquipment = _spanEquipmentByEquipmentId[newSpanEquipmentState.Id];

            if (!_spanEquipmentByEquipmentId.TryUpdate(newSpanEquipmentState.Id, newSpanEquipmentState, oldSpanEquipment))
                throw new ApplicationException($"Concurrency issue updating span equipment index. Span equipment id: {newSpanEquipmentState.Id} Please make sure that events are applied in sequence to the projection.");

            if (!_spanEquipmentByInterestId.TryUpdate(newSpanEquipmentState.WalkOfInterestId, newSpanEquipmentState, oldSpanEquipment))
                throw new ApplicationException($"Concurrency issue updating span equipment interest index. Span equipment id: {newSpanEquipmentState.Id} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryUpdate(NodeContainer newNodeContainerState)
        {
            var oldEquipment = _nodeContainerByEquipmentId[newNodeContainerState.Id];

            if (!_nodeContainerByEquipmentId.TryUpdate(newNodeContainerState.Id, newNodeContainerState, oldEquipment))
                throw new ApplicationException($"Concurrency issue updating node container equipment index. Node container equipment id: {newNodeContainerState.Id} Please make sure that events are applied in sequence to the projection.");

            if (!_nodeContainerByInterestId.TryUpdate(newNodeContainerState.InterestId, newNodeContainerState, oldEquipment))
                throw new ApplicationException($"Concurrency issue updating node container equipment interest index. Node container equipment id: {newNodeContainerState.Id} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryRemove(Guid spanEquipmentId, Guid spanEquipmentInterestId)
        {
            if (!_spanEquipmentByEquipmentId.TryRemove(spanEquipmentId, out _))
                throw new ApplicationException($"Concurrency issue removing span equipment index. Span equipment id: {spanEquipmentId} Please make sure that events are applied in sequence to the projection.");

            if (!_spanEquipmentByInterestId.TryRemove(spanEquipmentInterestId, out _))
                throw new ApplicationException($"Concurrency issue removing span equipment interest index. Span equipment id: {spanEquipmentId} Please make sure that events are applied in sequence to the projection.");
        }
    }
}
