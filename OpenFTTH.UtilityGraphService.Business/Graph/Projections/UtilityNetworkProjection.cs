﻿using DAX.EventProcessing;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Projections;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Concurrent;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityNetworkProjection : ProjectionBase
    {
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByEquipmentId = new ConcurrentDictionary<Guid, SpanEquipment>();
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByInterestId = new ConcurrentDictionary<Guid, SpanEquipment>();
        private readonly LookupCollection<NodeContainer> _nodeContainerByEquipmentId = new LookupCollection<NodeContainer>();
        private readonly ConcurrentDictionary<Guid, NodeContainer> _nodeContainerByInterestId = new ConcurrentDictionary<Guid, NodeContainer>();
        private readonly UtilityGraph _utilityGraph = new UtilityGraph();

        public UtilityGraph Graph => _utilityGraph;
    
        public LookupCollection<NodeContainer> NodeContainers => _nodeContainerByEquipmentId;

        public LookupCollection<SpanEquipment> SpanEquipments => new LookupCollection<SpanEquipment>(_spanEquipmentByEquipmentId.Values);
        
        public UtilityNetworkProjection(IExternalEventProducer externalEventProducer)
        {
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
            ProjectEvent<SpanEquipmentAffixedToContainer>(Project);
            ProjectEvent<SpanSegmentsCut>(Project);
            ProjectEvent<NodeContainerPlacedInRouteNetwork>(Project);
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

                case (SpanEquipmentAffixedToContainer @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (SpanSegmentsCut @event):
                    TryUpdate(SpanEquipmentProjectionFunctions.Apply(_spanEquipmentByEquipmentId[@event.SpanEquipmentId], @event));
                    break;

                case (NodeContainerPlacedInRouteNetwork @event):
                    StoreAndIndexVirginContainerEquipment(@event.Container);
                    break;
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

        private void StoreAndIndexVirginSpanEquipment(SpanEquipment spanEquipment)
        {
            // Store the new span equipment in memory
            _spanEquipmentByEquipmentId.TryAdd(spanEquipment.Id, spanEquipment);
            _spanEquipmentByInterestId.TryAdd(spanEquipment.WalkOfInterestId, spanEquipment);

            // Add span segments to the graph
            for (UInt16 structureIndex = 0; structureIndex < spanEquipment.SpanStructures.Length; structureIndex++)
            {
                // We're dealing with a virgin span equipment and therefore only disconnected segments
                _utilityGraph.AddDisconnectedSegment(spanEquipment, structureIndex);
            }
        }

        private void StoreAndIndexVirginContainerEquipment(NodeContainer nodeContainer)
        {
            // Store the new span equipment in memory
            _nodeContainerByEquipmentId.Add(nodeContainer);
            _nodeContainerByInterestId.TryAdd(nodeContainer.InterestId, nodeContainer);
        }

    }
}