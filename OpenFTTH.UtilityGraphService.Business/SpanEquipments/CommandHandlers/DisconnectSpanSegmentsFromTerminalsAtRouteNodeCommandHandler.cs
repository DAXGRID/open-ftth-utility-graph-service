using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class DisconnectSpanSegmentsFromTerminalsAtRouteNodeCommandHandler : ICommandHandler<DisconnectSpanSegmentsFromTerminalsAtRouteNode, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;

        public DisconnectSpanSegmentsFromTerminalsAtRouteNodeCommandHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
        }

        public Task<Result> HandleAsync(DisconnectSpanSegmentsFromTerminalsAtRouteNode command)
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            if (command.Disconnects.Length == 0)
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.INVALID_SPAN_DISCONNECT_LIST_CANNOT_BE_EMPTY, "The list of span and terminals to disconnect cannot be empty")));

            // Lookup the span equipment
            if (!utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.Disconnects[0].SpanSegmentId, out var spanSegmentRef))
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND, $"Cannot find any span segment in the utility graph with id: {command.Disconnects[0].SpanSegmentId}")));

            var spanEquipment = spanSegmentRef.SpanEquipment(utilityNetwork);
            var spanSegment = spanSegmentRef.SpanSegment(utilityNetwork);

            // Check that span segment is connected to route node
            if (spanEquipment.NodesOfInterestIds[spanSegment.FromNodeOfInterestIndex] != command.RouteNodeId
                && spanEquipment.NodesOfInterestIds[spanSegment.ToNodeOfInterestIndex] != command.RouteNodeId)
            {
                return Task.FromResult(Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_IS_NOT_RELATED_TO_ROUTE_NODE, $"The span segment with id: {spanSegment.Id} is not related to route node: {command.RouteNodeId} in any way. Please check command arguments.")));
            }


            // Disconnect the first span equipment from the terminal
            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(spanEquipment.Id);

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var firstSpanEquipmentConnectResult = spanEquipmentAR.DisconnectSegmentsFromTerminals(
                cmdContext: commandContext,
                command.Disconnects
            );

            _eventStore.Aggregates.Store(spanEquipmentAR);

            NotifyExternalServicesAboutChange(command.RouteNodeId, new Guid[] { spanEquipment.Id });

            return Task.FromResult(Result.Ok());
        }

        private async void NotifyExternalServicesAboutChange(Guid routeNodeId, Guid[] spanEquipmentIds)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Modification, spanEquipmentIds)
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

  