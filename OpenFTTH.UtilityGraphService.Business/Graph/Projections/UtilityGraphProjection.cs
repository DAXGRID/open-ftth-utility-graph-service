using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.Core;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Concurrent;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraphProjection : ProjectionBase
    {
        private readonly LookupCollection<SpanEquipment> _spanEquipmentByEquipmentId = new LookupCollection<SpanEquipment>();
        private readonly ConcurrentDictionary<Guid, SpanEquipment> _spanEquipmentByInterestId = new ConcurrentDictionary<Guid, SpanEquipment>();
        private readonly LookupCollection<NodeContainer> _nodeContainerByEquipmentId = new LookupCollection<NodeContainer>();
        private readonly ConcurrentDictionary<Guid, NodeContainer> _nodeContainerByInterestId = new ConcurrentDictionary<Guid, NodeContainer>();
        private readonly UtilityGraph _utilityGraph = new UtilityGraph();

        public LookupCollection<SpanEquipment> SpanEquipments => _spanEquipmentByEquipmentId;

        public LookupCollection<NodeContainer> NodeContainers => _nodeContainerByEquipmentId;

        public UtilityGraphProjection(IExternalEventProducer externalEventProducer)
        {
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
            ProjectEvent<NodeContainerPlacedInRouteNetwork>(Project);
        }

        public Result<IEquipment> GetEquipment(Guid equipmentOrInterestId)
        {
            if (_spanEquipmentByEquipmentId.TryGetValue(equipmentOrInterestId, out SpanEquipment? spanEquipmentByEquipmentId))
            {
                return Result.Ok<IEquipment>(spanEquipmentByEquipmentId);
            }
            else if (_spanEquipmentByInterestId.TryGetValue(equipmentOrInterestId, out SpanEquipment? spanEquipmentByInterestId))
            {
                return Result.Ok<IEquipment>(spanEquipmentByInterestId);
            }
            else if (_nodeContainerByEquipmentId.TryGetValue(equipmentOrInterestId, out NodeContainer? nodeContainerByEquipmentId))
            {
                return Result.Ok<IEquipment>(nodeContainerByEquipmentId);
            }
            else if (_nodeContainerByInterestId.TryGetValue(equipmentOrInterestId, out NodeContainer? nodeContainerByInterestId))
            {
                return Result.Ok<IEquipment>(nodeContainerByInterestId);
            }
            else
            {
                return Result.Fail<IEquipment>($"No span equipment with id or interest id: {equipmentOrInterestId} found");
            }
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanEquipmentPlacedInRouteNetwork @event):
                    StoreVirginSpanEquipment(@event.Equipment);
                    break;

                case (NodeContainerPlacedInRouteNetwork @event):
                    StoreVirginContainerEquipment(@event.Container);
                    break;
            }
        }

        private void StoreVirginSpanEquipment(SpanEquipment spanEquipment)
        {
            // Store the new span equipment in memory
            _spanEquipmentByEquipmentId.Add(spanEquipment);
            _spanEquipmentByInterestId.TryAdd(spanEquipment.WalkOfInterestId, spanEquipment);

            // Add span segments to the graph
            for (UInt16 structureIndex = 0; structureIndex < spanEquipment.SpanStructures.Length; structureIndex++)
            {
                // We're dealing with a virgin span equipment and therefore only disconnected segments
                _utilityGraph.AddDisconnectedSegment(spanEquipment, structureIndex);
            }
        }

        private void StoreVirginContainerEquipment(NodeContainer nodeContainer)
        {
            // Store the new span equipment in memory
            _nodeContainerByEquipmentId.Add(nodeContainer);
            _nodeContainerByInterestId.TryAdd(nodeContainer.InterestId, nodeContainer);
        }

    }
}
