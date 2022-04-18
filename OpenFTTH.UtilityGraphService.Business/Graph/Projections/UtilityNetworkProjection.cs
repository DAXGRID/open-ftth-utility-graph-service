using DAX.EventProcessing;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Projections;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityNetworkProjection : ProjectionBase
    {
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByEquipmentId = new();
        private readonly ConcurrentDictionary<Guid, TerminalEquipment> _terminalEquipmentByEquipmentId = new();
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByInterestId = new();
        private readonly ConcurrentDictionary<Guid, NodeContainer> _nodeContainerByEquipmentId = new();
        private readonly ConcurrentDictionary<Guid, NodeContainer> _nodeContainerByInterestId = new();
        private readonly ConcurrentDictionary<Guid, List<Guid>> _relatedCablesByConduitSegmentId = new();

        private readonly UtilityGraph _utilityGraph;

        public UtilityGraph Graph => _utilityGraph;

        public IReadOnlyDictionary<Guid, NodeContainer> NodeContainerByEquipmentId => _nodeContainerByEquipmentId;

        public IReadOnlyDictionary<Guid, NodeContainer> NodeContainerByInterestId => _nodeContainerByInterestId;

        public IReadOnlyDictionary<Guid, SpanEquipment> SpanEquipmentsByEquipmentId => _spanEquipmentByEquipmentId;

        public IReadOnlyDictionary<Guid, SpanEquipment> SpanEquipmentsByInterestId => _spanEquipmentByInterestId;

        public IReadOnlyDictionary<Guid, TerminalEquipment> TerminalEquipmentByEquipmentId => _terminalEquipmentByEquipmentId;

        public IReadOnlyDictionary<Guid, List<Guid>> RelatedCablesByConduitSegmentId => _relatedCablesByConduitSegmentId;


        public UtilityNetworkProjection()
        {
            _utilityGraph = new(this);

            // Span equipment
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
            ProjectEvent<SpanEquipmentAffixedToContainer>(Project);
            ProjectEvent<SpanEquipmentAffixSideChanged>(Project);
            ProjectEvent<SpanEquipmentDetachedFromContainer>(Project);
            ProjectEvent<SpanSegmentsCut>(Project);
            ProjectEvent<SpanEquipmentCutReverted>(Project);
            ProjectEvent<SpanSegmentsConnectedToSimpleTerminals>(Project);
            ProjectEvent<SpanSegmentDisconnectedFromTerminal>(Project);
            ProjectEvent<SpanSegmentsDisconnectedFromTerminals>(Project);
            ProjectEvent<AdditionalStructuresAddedToSpanEquipment>(Project);
            ProjectEvent<SpanStructureRemoved>(Project);
            ProjectEvent<SpanEquipmentRemoved>(Project);
            ProjectEvent<SpanEquipmentMoved>(Project);
            ProjectEvent<SpanEquipmentMerged>(Project);
            ProjectEvent<SpanEquipmentMarkingInfoChanged>(Project);
            ProjectEvent<SpanEquipmentAddressInfoChanged>(Project);
            ProjectEvent<SpanEquipmentManufacturerChanged>(Project);
            ProjectEvent<SpanEquipmentSpecificationChanged>(Project);
            ProjectEvent<SpanEquipmentAffixedToParent>(Project);
            ProjectEvent<SpanEquipmentDetachedFromParent>(Project);

            // Terminal equipment
            ProjectEvent<TerminalEquipmentPlacedInNodeContainer>(Project);
            ProjectEvent<TerminalEquipmentNamingInfoChanged>(Project);
            ProjectEvent<TerminalEquipmentAddressInfoChanged>(Project);
            ProjectEvent<TerminalEquipmentManufacturerChanged>(Project);
            ProjectEvent<TerminalEquipmentSpecificationChanged>(Project);
            ProjectEvent<TerminalEquipmentRemoved>(Project);

            // Node container
            ProjectEvent<NodeContainerPlacedInRouteNetwork>(Project);
            ProjectEvent<NodeContainerRemovedFromRouteNetwork>(Project);
            ProjectEvent<NodeContainerManufacturerChanged>(Project);
            ProjectEvent<NodeContainerSpecificationChanged>(Project);
            ProjectEvent<NodeContainerVerticalAlignmentReversed>(Project);
            ProjectEvent<NodeContainerRackAdded>(Project);
            ProjectEvent<NodeContainerRackRemoved>(Project);
            ProjectEvent<NodeContainerRackSpecificationChanged>(Project);
            ProjectEvent<NodeContainerRackNameChanged>(Project);
            ProjectEvent<NodeContainerRackHeightInUnitsChanged>(Project);
            ProjectEvent<NodeContainerTerminalEquipmentAdded>(Project);
            ProjectEvent<NodeContainerTerminalEquipmentsAddedToRack>(Project);
            ProjectEvent<NodeContainerTerminalEquipmentReferenceRemoved>(Project);
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
            else if (_terminalEquipmentByEquipmentId.TryGetValue(equipmentOrInterestId, out TerminalEquipment? terminalEquipmentByEquipmentId))
            {
                if (terminalEquipmentByEquipmentId is T)
                {
                    equipment = (T)(object)terminalEquipmentByEquipmentId;
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
                // Span equipment events
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

                case (SpanEquipmentAffixSideChanged @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentDetachedFromContainer @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanSegmentsCut @event):
                    ProcesstSegmentCuts(@event);
                    break;

                case (SpanEquipmentCutReverted @event):
                    ProcesstSpanEquipmentCutReverted(@event);
                    break;

                case (SpanSegmentsConnectedToSimpleTerminals @event):
                    ProcessSegmentConnects(@event);
                    break;

                case (SpanSegmentDisconnectedFromTerminal @event):
                    ProcessSegmentDisconnects(@event);
                    break;

                case (SpanSegmentsDisconnectedFromTerminals @event):
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

                case (SpanEquipmentAddressInfoChanged @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentManufacturerChanged @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanEquipmentSpecificationChanged @event):
                    ProcessSpanEquipmentSpecificationChange(@event);
                    break;

                case (SpanEquipmentAffixedToParent @event):
                   ProcessSpanEquipmentParentAffix(@event);
                   break;

                case (SpanEquipmentDetachedFromParent @event):
                    ProcessSpanEquipmentParentDetach(@event);
                    break;


                // Terminal equipment events
                case (TerminalEquipmentPlacedInNodeContainer @event):
                    StoreAndIndexVirginTerminalEquipment(@event.Equipment);
                    break;

                case (TerminalEquipmentNamingInfoChanged @event):
                    TryUpdate(TerminalEquipmentProjectionFunctions.Apply(_terminalEquipmentByEquipmentId[@event.TerminalEquipmentId], @event));
                    break;

                case (TerminalEquipmentAddressInfoChanged @event):
                    TryUpdate(TerminalEquipmentProjectionFunctions.Apply(_terminalEquipmentByEquipmentId[@event.TerminalEquipmentId], @event));
                    break;

                case (TerminalEquipmentManufacturerChanged @event):
                    TryUpdate(TerminalEquipmentProjectionFunctions.Apply(_terminalEquipmentByEquipmentId[@event.TerminalEquipmentId], @event));
                    break;

                case (TerminalEquipmentSpecificationChanged @event):
                    TryUpdate(TerminalEquipmentProjectionFunctions.Apply(_terminalEquipmentByEquipmentId[@event.TerminalEquipmentId], @event));
                    break;

                case (TerminalEquipmentRemoved @event):
                    ProcessTerminalEquipmentRemoval(@event);
                    break;


                // Node container events
                case (NodeContainerPlacedInRouteNetwork @event):
                    StoreAndIndexVirginContainerEquipment(@event.Container);
                    break;

                case (NodeContainerRemovedFromRouteNetwork @event):
                    ProcessNodeContainerRemoval(@event);
                    break;

                case (NodeContainerVerticalAlignmentReversed @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerManufacturerChanged @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerSpecificationChanged @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerRackAdded @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerRackRemoved @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerRackSpecificationChanged @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerRackNameChanged @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerRackHeightInUnitsChanged @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerTerminalEquipmentAdded @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerTerminalEquipmentsAddedToRack @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;

                case (NodeContainerTerminalEquipmentReferenceRemoved @event):
                    TryUpdate(NodeContainerProjectionFunctions.Apply(_nodeContainerByEquipmentId[@event.NodeContainerId], @event));
                    break;
            }
        }

        private void ProcessSpanEquipmentParentDetach(SpanEquipmentDetachedFromParent @event)
        {
            var existingSpanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var newSpanEquipment = SpanEquipmentProjectionFunctions.Apply(existingSpanEquipment, @event);
            TryUpdate(newSpanEquipment);

            UpdateRelatedCableIndex(@event.NewUtilityHopList, existingSpanEquipment);
        }

        private void ProcessSpanEquipmentParentAffix(SpanEquipmentAffixedToParent @event)
        {
            var existingSpanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var newSpanEquipment = SpanEquipmentProjectionFunctions.Apply(existingSpanEquipment, @event);
            TryUpdate(newSpanEquipment);

            UpdateRelatedCableIndex(@event.NewUtilityHopList, existingSpanEquipment);
        }

        private void UpdateRelatedCableIndex(UtilityNetworkHop[] newUtilityNetworkHopList, SpanEquipment existingSpanEquipment)
        {
            // Update segment to cable index
            HashSet<Guid> existingSegmentWhereToRemoveCableRel = new();

            if (existingSpanEquipment.UtilityNetworkHops != null && existingSpanEquipment.UtilityNetworkHops.Length > 0)
            {
                foreach (var utilityHop in existingSpanEquipment.UtilityNetworkHops)
                {
                    foreach (var affix in utilityHop.ParentAffixes)
                    {
                        existingSegmentWhereToRemoveCableRel.Add(affix.SpanSegmentId);
                    }
                }
            }

            HashSet<Guid> segmentIdsWhereToAddCableRel = new();

            foreach (var utilityHop in newUtilityNetworkHopList)
            {
                foreach (var affix in utilityHop.ParentAffixes)
                {
                    if (existingSegmentWhereToRemoveCableRel.Contains(affix.SpanSegmentId))
                    {
                        existingSegmentWhereToRemoveCableRel.Remove(affix.SpanSegmentId);
                    }
                    else
                    {
                        segmentIdsWhereToAddCableRel.Add(affix.SpanSegmentId);
                    }
                }
            }

            // Remove cable ids from index
            foreach (var segmentIdWhereToRemoveCabelRel in existingSegmentWhereToRemoveCableRel)
            {
                if (_relatedCablesByConduitSegmentId.ContainsKey(segmentIdWhereToRemoveCabelRel))
                {
                    var existingValue = _relatedCablesByConduitSegmentId[segmentIdWhereToRemoveCabelRel];

                    List<Guid> newValue = new();
                    foreach (var cableId in existingValue)
                    {
                        if (cableId != existingSpanEquipment.Id)
                            newValue.Add(cableId);
                    }

                    if (!_relatedCablesByConduitSegmentId.TryUpdate(segmentIdWhereToRemoveCabelRel, newValue, existingValue))
                        throw new ApplicationException($"Concurrent exception trying to update conduit segment to cable index. Cable with id: {existingSpanEquipment.Id}");
                }
            }


            // Add cable ids from index
            foreach (var segmentIdWhereToAddCabelRel in segmentIdsWhereToAddCableRel)
            {
                _relatedCablesByConduitSegmentId.AddOrUpdate(
                             segmentIdWhereToAddCabelRel,
                             new List<Guid> { existingSpanEquipment.Id },
                             (key, oldValue) =>
                             {
                                 var newList = new List<Guid> { existingSpanEquipment.Id };
                                 newList.AddRange(oldValue);
                                 return newList;
                             }
                          );
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

            // Index conduit relations
            if (spanEquipment.UtilityNetworkHops != null && spanEquipment.UtilityNetworkHops.Length > 0)
            {
                foreach (var utilityHop in spanEquipment.UtilityNetworkHops)
                {
                    foreach (var parentAffix in utilityHop.ParentAffixes)
                    {
                        _relatedCablesByConduitSegmentId.AddOrUpdate(
                            parentAffix.SpanSegmentId, 
                            new List<Guid> { spanEquipment.Id },
                            (key, oldValue) => {
                                var newList = new List<Guid> { spanEquipment.Id };
                                newList.AddRange(oldValue);
                                return newList; 
                            }
                         );
                    }
                }
            }
        }

        private void StoreAndIndexVirginTerminalEquipment(TerminalEquipment terminalEquipment)
        {
            // Store the new terminal equipment in memory
            _terminalEquipmentByEquipmentId.TryAdd(terminalEquipment.Id, terminalEquipment);

            var nodeContainer = _nodeContainerByEquipmentId[terminalEquipment.NodeContainerId];


            HashSet<Guid> internalNodes = new();

            // Add terminals to the graph
            for (UInt16 structureIndex = 0; structureIndex < terminalEquipment.TerminalStructures.Length; structureIndex++)
            {
                var terminalStructure = terminalEquipment.TerminalStructures[structureIndex];

                for (UInt16 terminalIndex = 0; terminalIndex < terminalStructure.Terminals.Length; terminalIndex++)
                {
                    var terminal = terminalStructure.Terminals[terminalIndex];

                    // We're dealing with a virgin terminal
                    _utilityGraph.AddDisconnectedTerminal(nodeContainer.RouteNodeId, terminalEquipment, terminal.Id, structureIndex, terminalIndex);

                    // Add eventually internal node
                    if (terminal.InternalConnectivityNodeId != null && terminal.InternalConnectivityNodeId != Guid.Empty)
                        internalNodes.Add(terminal.InternalConnectivityNodeId.Value);
                }
            }

            // If we're dealing with a terminal equipment with internal nodes, we need connect them in the graph
            if (internalNodes.Count > 0)
            {
                // First create all the internal nodes in the graph
                UtilityGraphTerminalEquipmentProjections.ApplyInternalConnectivityToGraph(nodeContainer, terminalEquipment, Graph);
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
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcesstSpanEquipmentCutReverted(SpanEquipmentCutReverted @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessSegmentConnects(SpanSegmentsConnectedToSimpleTerminals @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessSegmentDisconnects(SpanSegmentDisconnectedFromTerminal @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessSegmentDisconnects(SpanSegmentsDisconnectedFromTerminals @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessAdditionalStructures(AdditionalStructuresAddedToSpanEquipment @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessInnerStructureRemoval(SpanStructureRemoved @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }


        private void ProcessSpanEquipmentRemoval(SpanEquipmentRemoved @event)
        {
            var existingSpanEquipment = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];

            TryRemoveSpanEquipment(@event.SpanEquipmentId, existingSpanEquipment.WalkOfInterestId);

            // Remove span segments from the graph
            foreach (var spanStructure in existingSpanEquipment.SpanStructures)
            {
                if (!spanStructure.Deleted)
                {
                    foreach (var spanSegment in spanStructure.SpanSegments)
                    {
                        _utilityGraph.RemoveGraphElement(spanSegment.Id);
                    }
                }
            }
        }

        private void ProcessTerminalEquipmentRemoval(TerminalEquipmentRemoved @event)
        {
            var existingTerminalEquipment = _terminalEquipmentByEquipmentId[@event.TerminalEquipmentId];

            TryRemoveTerminalEquipment(@event.TerminalEquipmentId);

            // Remove terminals from the graph
            foreach (var terminalStructure in existingTerminalEquipment.TerminalStructures)
            {
                foreach (var terminal in terminalStructure.Terminals)
                {
                    _utilityGraph.RemoveGraphElement(terminal.Id);
                }
            }
        }


        private void ProcessNodeContainerRemoval(NodeContainerRemovedFromRouteNetwork @event)
        {
            var existingNodeContainer = _nodeContainerByEquipmentId[@event.NodeContainerId];

            TryRemoveNodeContainer(@event.NodeContainerId, existingNodeContainer.InterestId);
        }

        private void ProcessSpanEquipmentSpecificationChange(SpanEquipmentSpecificationChanged @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphSegmentProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessSpanEquipmentMerge(SpanEquipmentMerged @event)
        {
            TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
        }


        private void TryUpdate(SpanEquipment newSpanEquipmentState)
        {
            var oldSpanEquipment = _spanEquipmentByEquipmentId[newSpanEquipmentState.Id];

            if (!_spanEquipmentByEquipmentId.TryUpdate(newSpanEquipmentState.Id, newSpanEquipmentState, oldSpanEquipment))
                throw new ApplicationException($"Concurrency issue updating span equipment index. Span equipment id: {newSpanEquipmentState.Id} Please make sure that events are applied in sequence to the projection.");

            if (!_spanEquipmentByInterestId.TryUpdate(newSpanEquipmentState.WalkOfInterestId, newSpanEquipmentState, oldSpanEquipment))
                throw new ApplicationException($"Concurrency issue updating span equipment interest index. Span equipment id: {newSpanEquipmentState.Id} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryUpdate(TerminalEquipment newTerminalEquipmentState)
        {
            var oldTerminalEquipment = _terminalEquipmentByEquipmentId[newTerminalEquipmentState.Id];

            if (!_terminalEquipmentByEquipmentId.TryUpdate(newTerminalEquipmentState.Id, newTerminalEquipmentState, oldTerminalEquipment))
                throw new ApplicationException($"Concurrency issue updating terminal equipment index. Terminal equipment id: {newTerminalEquipmentState.Id} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryUpdate(NodeContainer newNodeContainerState)
        {
            var oldEquipment = _nodeContainerByEquipmentId[newNodeContainerState.Id];

            if (!_nodeContainerByEquipmentId.TryUpdate(newNodeContainerState.Id, newNodeContainerState, oldEquipment))
                throw new ApplicationException($"Concurrency issue updating node container equipment index. Node container equipment id: {newNodeContainerState.Id} Please make sure that events are applied in sequence to the projection.");

            if (!_nodeContainerByInterestId.TryUpdate(newNodeContainerState.InterestId, newNodeContainerState, oldEquipment))
                throw new ApplicationException($"Concurrency issue updating node container equipment interest index. Node container equipment id: {newNodeContainerState.Id} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryRemoveSpanEquipment(Guid spanEquipmentId, Guid spanEquipmentInterestId)
        {
            if (!_spanEquipmentByEquipmentId.TryRemove(spanEquipmentId, out _))
                throw new ApplicationException($"Concurrency issue removing span equipment index. Span equipment id: {spanEquipmentId} Please make sure that events are applied in sequence to the projection.");

            if (!_spanEquipmentByInterestId.TryRemove(spanEquipmentInterestId, out _))
                throw new ApplicationException($"Concurrency issue removing span equipment interest index. Span equipment id: {spanEquipmentId} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryRemoveTerminalEquipment(Guid terminalEquipmentId)
        {
            if (!_terminalEquipmentByEquipmentId.TryRemove(terminalEquipmentId, out _))
                throw new ApplicationException($"Concurrency issue removing teminal equipment from index. Terminal equipment id: {terminalEquipmentId} Please make sure that events are applied in sequence to the projection.");
        }

        private void TryRemoveNodeContainer(Guid nodeContainertId, Guid nodeContainerInterestId)
        {
            if (!_nodeContainerByEquipmentId.TryRemove(nodeContainertId, out _))
                throw new ApplicationException($"Concurrency issue removing node container from equipment dictionary. Node container with id: {nodeContainertId} Please make sure that events are applied in sequence to the projection.");

            if (!_nodeContainerByInterestId.TryRemove(nodeContainerInterestId, out _))
                throw new ApplicationException($"Concurrency issue removing node container from interest dictionary. Span equipment id: {nodeContainertId} Please make sure that events are applied in sequence to the projection.");
        }
    }
}
