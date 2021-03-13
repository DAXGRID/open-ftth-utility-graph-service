using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class DisconnectSpanSegmentsCommandHandler : ICommandHandler<DisconnectSpanSegmentsAtRouteNode, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;

        public DisconnectSpanSegmentsCommandHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
        }

        public Task<Result> HandleAsync(DisconnectSpanSegmentsAtRouteNode command)
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            if (command.SpanSegmentsToDisconnect.Length != 2)
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.INVALID_SPAN_SEGMENT_LIST_MUST_CONTAIN_TWO_SPAN_SEGMENT_IDS, "The list of span segments to connect must container two span segment ids.")));

            // Because the client do not provide the span equipment ids, but span segment ids only,
            // we need lookup the span equipments via the the utility network graph
            SpanEquipment[] spanEquipmentsToDisconnect = new SpanEquipment[2];

            // Lookup the first span equipment
            if (!utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.SpanSegmentsToDisconnect[0], out var firstSpanSegmentGraphElement))
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND, $"Cannot find any span segment in the utility graph with id: {command.SpanSegmentsToDisconnect[0]}")));

            // Lookup the second span equipment
            if (!utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.SpanSegmentsToDisconnect[1], out var secondSpanSegmentGraphElement))
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND, $"Cannot find any span segment in the utility graph with id: {command.SpanSegmentsToDisconnect[1]}")));

            // Check that first span segment is connected to route node
            if (firstSpanSegmentGraphElement.SpanEquipment.NodesOfInterestIds[firstSpanSegmentGraphElement.SpanSegment.FromNodeOfInterestIndex] != command.RouteNodeId
                && firstSpanSegmentGraphElement.SpanEquipment.NodesOfInterestIds[firstSpanSegmentGraphElement.SpanSegment.ToNodeOfInterestIndex] != command.RouteNodeId)
            {
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_IS_NOT_RELATED_TO_ROUTE_NODE, $"The span segment with id: {firstSpanSegmentGraphElement.SpanSegment.Id} is not related to route node: {command.RouteNodeId} in any way. Please check command arguments.")));
            }

            // Check that second span segment is connected to route node
            if (secondSpanSegmentGraphElement.SpanEquipment.NodesOfInterestIds[secondSpanSegmentGraphElement.SpanSegment.FromNodeOfInterestIndex] != command.RouteNodeId
                && secondSpanSegmentGraphElement.SpanEquipment.NodesOfInterestIds[secondSpanSegmentGraphElement.SpanSegment.ToNodeOfInterestIndex] != command.RouteNodeId)
            {
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_IS_NOT_RELATED_TO_ROUTE_NODE, $"The span segment with id: {secondSpanSegmentGraphElement.SpanSegment.Id} is not related to route node: {command.RouteNodeId} in any way. Please check command arguments.")));
            }

            // Check that the two segments are connected
            HashSet<Guid> firstSegmentTerminalIds = new HashSet<Guid>();
            if (firstSpanSegmentGraphElement.SpanSegment.FromTerminalId != Guid.Empty)
                firstSegmentTerminalIds.Add(firstSpanSegmentGraphElement.SpanSegment.FromTerminalId);
            if (firstSpanSegmentGraphElement.SpanSegment.ToTerminalId != Guid.Empty)
                firstSegmentTerminalIds.Add(firstSpanSegmentGraphElement.SpanSegment.ToTerminalId);

            Guid sharedTerminalId = Guid.Empty;

            if (firstSegmentTerminalIds.Contains(secondSpanSegmentGraphElement.SpanSegment.FromTerminalId))
                sharedTerminalId = secondSpanSegmentGraphElement.SpanSegment.FromTerminalId;
            else if (firstSegmentTerminalIds.Contains(secondSpanSegmentGraphElement.SpanSegment.ToTerminalId))
                sharedTerminalId = secondSpanSegmentGraphElement.SpanSegment.ToTerminalId;

            if (sharedTerminalId == Guid.Empty)
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENTS_ARE_NOT_CONNECTED, $"The span segment with id: {firstSpanSegmentGraphElement.SpanSegment.Id} and The span segment with id: {secondSpanSegmentGraphElement.SpanSegment.Id} is not connected in route node: {command.RouteNodeId}. Please check command arguments.")));


            // Disconnect the first span equipment from the terminal
            var firstSpanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(firstSpanSegmentGraphElement.SpanEquipment.Id);

            var firstSpanEquipmentConnectResult = firstSpanEquipmentAR.DisconnectSegmentFromTerminal(
                spanSegmentId: firstSpanSegmentGraphElement.SpanSegment.Id,
                terminalId: sharedTerminalId
            );

            if (!firstSpanEquipmentConnectResult.IsSuccess)
                return Task.FromResult(firstSpanEquipmentConnectResult);

            // Disconnect the second span equipment from the terminal
            var secondSpanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(secondSpanSegmentGraphElement.SpanEquipment.Id);

            var secondSpanEquipmentConnectResult = secondSpanEquipmentAR.DisconnectSegmentFromTerminal(
                spanSegmentId: secondSpanSegmentGraphElement.SpanSegment.Id,
                terminalId: sharedTerminalId
            );

            if (!secondSpanEquipmentConnectResult.IsSuccess)
                return Task.FromResult(secondSpanEquipmentConnectResult);

            _eventStore.Aggregates.Store(firstSpanEquipmentAR);
            _eventStore.Aggregates.Store(secondSpanEquipmentAR);

            NotifyExternalServicesAboutChange(firstSpanSegmentGraphElement.SpanEquipment.Id, secondSpanSegmentGraphElement.SpanEquipment.Id, command.RouteNodeId);

            return Task.FromResult(Result.Ok());
        }

        private async void NotifyExternalServicesAboutChange(Guid firstSpanEquipmentId, Guid secondSpanEquipmentId, Guid routeNodeId)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Modification, new Guid[] { firstSpanEquipmentId, secondSpanEquipmentId })
            };

            var updatedEvent =
                new RouteNetworkElementContainedEquipmentUpdated(
                    eventType: typeof(RouteNetworkElementContainedEquipmentUpdated).Name,
                    eventId: Guid.NewGuid(),
                    eventTimestamp: DateTime.UtcNow,
                    applicationName: "UtilityNetworkService",
                    applicationInfo: null,
                    category: "EquipmentConnectivityModification.Disconnect",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: new Guid[] { routeNodeId }
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }
    }
}

  