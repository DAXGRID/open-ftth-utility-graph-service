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

        private readonly UtilityGraph _utilityGraph;

        public UtilityGraph Graph => _utilityGraph;

        public LookupCollection<NodeContainer> NodeContainers => new LookupCollection<NodeContainer>(_nodeContainerByEquipmentId.Values);

        public LookupCollection<SpanEquipment> SpanEquipments => new LookupCollection<SpanEquipment>(_spanEquipmentByEquipmentId.Values);
        
        public UtilityNetworkProjection()
        {
            _utilityGraph = new(this);

            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
            ProjectEvent<SpanEquipmentAffixedToContainer>(Project);
            ProjectEvent<SpanEquipmentAffixSideChanged>(Project);
            ProjectEvent<SpanEquipmentDetachedFromContainer>(Project);
            ProjectEvent<SpanSegmentsCut>(Project);
            ProjectEvent<SpanEquipmentCutReverted>(Project);
            ProjectEvent<SpanSegmentsConnectedToSimpleTerminals>(Project);
            ProjectEvent<SpanSegmentDisconnectedFromTerminal>(Project);
            ProjectEvent<AdditionalStructuresAddedToSpanEquipment>(Project);
            ProjectEvent<SpanStructureRemoved>(Project);
            ProjectEvent<SpanEquipmentRemoved>(Project);
            ProjectEvent<SpanEquipmentMoved>(Project);
            ProjectEvent<SpanEquipmentMerged>(Project);
            ProjectEvent<SpanEquipmentMarkingInfoChanged>(Project);
            ProjectEvent<SpanEquipmentAddressInfoChanged>(Project);
            ProjectEvent<SpanEquipmentManufacturerChanged>(Project);
            ProjectEvent<SpanEquipmentSpecificationChanged>(Project);

            ProjectEvent<NodeContainerPlacedInRouteNetwork>(Project);
            ProjectEvent<NodeContainerRemovedFromRouteNetwork>(Project);
            ProjectEvent<NodeContainerManufacturerChanged>(Project);
            ProjectEvent<NodeContainerSpecificationChanged>(Project);
            ProjectEvent<NodeContainerVerticalAlignmentReversed>(Project);
            ProjectEvent<RackAddedToNodeContainer>(Project);
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

                case (RackAddedToNodeContainer @event):
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
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcesstSpanEquipmentCutReverted(SpanEquipmentCutReverted @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessSegmentConnects(SpanSegmentsConnectedToSimpleTerminals @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessSegmentDisconnects(SpanSegmentDisconnectedFromTerminal @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessAdditionalStructures(AdditionalStructuresAddedToSpanEquipment @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
        }

        private void ProcessInnerStructureRemoval(SpanStructureRemoved @event)
        {
            var before = _spanEquipmentByEquipmentId[@event.SpanEquipmentId];
            var after = SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event);
            TryUpdate(after);

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
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
                        _utilityGraph.RemoveDisconnectedSegment(spanSegment.Id);
                    }
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

            UtilityGraphProjections.ApplyConnectivityChangesToGraph(before, after, _utilityGraph);
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

        private void TryRemoveNodeContainer(Guid nodeContainertId, Guid nodeContainerInterestId)
        {
            if (!_nodeContainerByEquipmentId.TryRemove(nodeContainertId, out _))
                throw new ApplicationException($"Concurrency issue removing node container from equipment dictionary. Node container with id: {nodeContainertId} Please make sure that events are applied in sequence to the projection.");

            if (!_nodeContainerByInterestId.TryRemove(nodeContainerInterestId, out _))
                throw new ApplicationException($"Concurrency issue removing node container from interest dictionary. Span equipment id: {nodeContainertId} Please make sure that events are applied in sequence to the projection.");
        }
    }
}
