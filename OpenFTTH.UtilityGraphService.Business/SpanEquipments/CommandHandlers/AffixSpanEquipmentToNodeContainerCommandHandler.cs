using DAX.EventProcessing;
using FluentResults;
using Newtonsoft.Json;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class AffixSpanEquipmentToNodeContainerCommandHandler : ICommandHandler<AffixSpanEquipmentToNodeContainer, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;

        public AffixSpanEquipmentToNodeContainerCommandHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _externalEventProducer = externalEventProducer;
            _queryDispatcher = queryDispatcher;
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(AffixSpanEquipmentToNodeContainer command)
        {
            if (command.SpanSegmentId == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToNodeContainerError(AffixSpanEquipmentToNodeContainerErrorCodes.INVALID_SPAN_SEGMENT_ID_CANNOT_BE_EMPTY, $"Span segment id must be specified.")));

            if (command.NodeContainerId == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToNodeContainerError(AffixSpanEquipmentToNodeContainerErrorCodes.INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY, $"Node container id must be specified.")));

            var _utilityNetwork = _eventStore.Projections.Get<UtilityGraphProjection>();

            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.SpanSegmentId, out var spanSegmentGraphElement))
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToNodeContainerError(AffixSpanEquipmentToNodeContainerErrorCodes.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment in the utility graph with id: {command.SpanSegmentId}")));

            if (!_utilityNetwork.TryGetEquipment<NodeContainer>(command.NodeContainerId, out var nodeContainer))
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToNodeContainerError(AffixSpanEquipmentToNodeContainerErrorCodes.INVALID_SPAN_CONTAINER_ID_NOT_FOUND, $"Cannot find any node container with id: {command.NodeContainerId}")));

            // Get interest information for both span equipment and node container, which is needed for the aggregate to validate the command
            var interestQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(new GetRouteNetworkDetails(new InterestIdList() { spanSegmentGraphElement.SpanEquipment.WalkOfInterestId, nodeContainer.InterestId })).Result;

            if (interestQueryResult.IsFailed)
                throw new ApplicationException($"Got unexpected error result: {interestQueryResult.Errors.First().Message} trying to query interest information for node container and/or span equipment while processing the AffixSpanEquipmentToNodeContainer command: " + JsonConvert.SerializeObject(command));

            if (interestQueryResult.Value.Interests == null)
                throw new ApplicationException("No data were unexpectedly returned trying to query interest information for node container and/or span equipment while processing the AffixSpanEquipmentToNodeContainer command: " + JsonConvert.SerializeObject(command));

            if (!interestQueryResult.Value.Interests.TryGetValue(spanSegmentGraphElement.SpanEquipment.WalkOfInterestId, out _))
                throw new ApplicationException($"No interest information were unexpectedly returned querying span equipment with id: {spanSegmentGraphElement.SpanEquipment.Id} interest id: {spanSegmentGraphElement.SpanEquipment.WalkOfInterestId}");

            if (!interestQueryResult.Value.Interests.TryGetValue(nodeContainer.InterestId, out _))
                throw new ApplicationException($"No interest information were unexpectedly returned querying node container with id: {nodeContainer.Id} interest id: {nodeContainer.InterestId}");

            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(spanSegmentGraphElement.SpanEquipment.Id);

            var affixResult = spanEquipmentAR.AffixToNodeContainer(
                spanEquipmentInterest: interestQueryResult.Value.Interests[spanSegmentGraphElement.SpanEquipment.WalkOfInterestId],
                nodeContainerRouteNodeId: interestQueryResult.Value.Interests[nodeContainer.InterestId].RouteNetworkElementRefs[0],
                nodeContainerId : command.NodeContainerId,
                spanSegmentId: command.SpanSegmentId,
                nodeContainerIngoingSide: command.NodeContainerIngoingSide
            );

            _eventStore.Aggregates.Store(spanEquipmentAR);

            return Task.FromResult(Result.Ok());
        }

        private async void NotifyExternalServicesAboutChange(Guid spanEquipmentId, Guid[] affectedRouteNetworkElementIds)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Modification, new Guid[] { spanEquipmentId })
            };

            var updatedEvent =
                new RouteNetworkElementContainedEquipmentUpdated(
                    eventType: typeof(RouteNetworkElementContainedEquipmentUpdated).Name,
                    eventId: Guid.NewGuid(),
                    eventTimestamp: DateTime.UtcNow,
                    applicationName: "UtilityNetworkService",
                    applicationInfo: null,
                    category: "EquipmentModification",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: affectedRouteNetworkElementIds
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);

        }
    }
}

  