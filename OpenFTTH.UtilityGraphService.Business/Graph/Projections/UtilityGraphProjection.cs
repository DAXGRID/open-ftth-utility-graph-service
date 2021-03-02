using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
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
        private readonly UtilityGraph _utilityGraph = new UtilityGraph();

        public LookupCollection<SpanEquipment> SpanEquipments => _spanEquipmentByEquipmentId;

        public UtilityGraphProjection(IExternalEventProducer externalEventProducer)
        {
            ProjectEvent<SpanEquipmentPlacedInRouteNetwork>(Project);
        }

        public Result<SpanEquipment> GetEquipment(Guid spanEquipmentOrInterestId)
        {
            if (_spanEquipmentByEquipmentId.TryGetValue(spanEquipmentOrInterestId, out SpanEquipment? spanEquipmentByEquipmentId))
            {
                return Result.Ok<SpanEquipment>(spanEquipmentByEquipmentId);
            }
            else if (_spanEquipmentByInterestId.TryGetValue(spanEquipmentOrInterestId, out SpanEquipment? spanEquipmentByInterestId))
            {
                return Result.Ok<SpanEquipment>(spanEquipmentByInterestId);
            }
            else
            {
                return Result.Fail<SpanEquipment>($"No span equipment with id or interest id: {spanEquipmentOrInterestId} found");
            }
        }

        private void Project(IEventEnvelope eventEnvelope)
        {
            switch (eventEnvelope.Data)
            {
                case (SpanEquipmentPlacedInRouteNetwork @event):
                    StoreVirginSpanEquipment(@event.Equipment);
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
    }
}
