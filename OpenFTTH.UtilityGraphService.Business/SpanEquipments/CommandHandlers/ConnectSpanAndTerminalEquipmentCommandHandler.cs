using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class ConnectSpanAndTerminalEquipmentCommandHandler : ICommandHandler<ConnectSpanEquipmentAndTerminalEquipment, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public ConnectSpanAndTerminalEquipmentCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result> HandleAsync(ConnectSpanEquipmentAndTerminalEquipment command)
        {
            if (command.SpanSegmentsIds.Length == 0)
                return Task.FromResult(Result.Fail(new ConnectSpanEquipmentAndTerminalEquipmentError(ConnectSpanEquipmentAndTerminalEquipmentErrorCodes.INVALID_SPAN_SEGMENT_LIST_CANNOT_BE_EMPTY, "A list of span segments to connect must be provided.")));

            if (command.TerminalIds.Length == 0)
                return Task.FromResult(Result.Fail(new ConnectSpanEquipmentAndTerminalEquipmentError(ConnectSpanEquipmentAndTerminalEquipmentErrorCodes.INVALID_TERMINAL_LIST_CANNOT_BE_EMPTY, "A list of terminals to connect must be provided.")));

            if (command.TerminalIds.Length != command.SpanSegmentsIds.Length)
                return Task.FromResult(Result.Fail(new ConnectSpanEquipmentAndTerminalEquipmentError(ConnectSpanEquipmentAndTerminalEquipmentErrorCodes.INVALID_SPAN_SEGMENT_LIST_AMOUNT_MUST_BE_EQUAL_TERMINAL_LIST_AMOUNT, "The number of span segment ids and terminal ids must be the same. Are connected one to one.")));

            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.SpanSegmentsIds[0], out var firstSpanSegmentGraphElement))
                return Task.FromResult(Result.Fail(new ConnectSpanEquipmentAndTerminalEquipmentError(ConnectSpanEquipmentAndTerminalEquipmentErrorCodes.SPAN_SEGMENT_NOT_FOUND, $"Cannot find any span segment in the utility graph with id: {command.SpanSegmentsIds[0]}")));

            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(command.TerminalIds[0], out var firstTerminalGraphElement))
                return Task.FromResult(Result.Fail(new ConnectSpanEquipmentAndTerminalEquipmentError(ConnectSpanEquipmentAndTerminalEquipmentErrorCodes.TERMINAL_NOT_FOUND, $"Cannot find any terminal in the utility graph with id: {command.TerminalIds[0]}")));

            var spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            var cmdContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(firstSpanSegmentGraphElement.SpanEquipmentId);

            var spanEquipmentConnectResult = spanEquipmentAR.ConnectCableSpanSegmentsWithTerminals(
                cmdContext: cmdContext,
                routeNodeId: command.RouteNodeId,
                specification: spanEquipmentSpecifications[firstSpanSegmentGraphElement.SpanEquipment(_utilityNetwork).SpecificationId],
                connects: BuildConnects(command.SpanSegmentsIds, command.TerminalIds)
            );

            if (spanEquipmentConnectResult.IsFailed)
                return Task.FromResult(Result.Fail(spanEquipmentConnectResult.Errors.First()));

            _eventStore.Aggregates.Store(spanEquipmentAR);

            return Task.FromResult(Result.Ok());
        }

        private SpanSegmentToSimpleTerminalConnectInfo[] BuildConnects(Guid[] segmentIds, Guid[] terminalIds)
        {
            List<SpanSegmentToSimpleTerminalConnectInfo> connects = new();

            for (int i = 0; i < segmentIds.Length; i++)
            {
                connects.Add(new SpanSegmentToSimpleTerminalConnectInfo(segmentIds[i], terminalIds[i]));
            }

            return connects.ToArray();
        }

        private ValidatedRouteNetworkWalk GetInterestInformation(SpanEquipment spanEquipment)
        {
            // Get interest information from existing span equipment
            var interestQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(new GetRouteNetworkDetails(new InterestIdList() { spanEquipment.WalkOfInterestId })).Result;

            if (interestQueryResult.IsFailed)
                throw new ApplicationException($"Got unexpected error result: {interestQueryResult.Errors.First().Message} trying to query interest information for span equipment: {spanEquipment.Id} walk of interest id: {spanEquipment.WalkOfInterestId}");

            if (interestQueryResult.Value.Interests == null)
                throw new ApplicationException($"Error querying interest information belonging to span equipment with id: {spanEquipment.Id}. No interest information returned from route network service.");

            if (!interestQueryResult.Value.Interests.TryGetValue(spanEquipment.WalkOfInterestId, out var routeNetworkInterest))
                throw new ApplicationException($"Error querying interest information belonging to span equipment with id: {spanEquipment.Id}. No interest information returned from route network service.");

            return new ValidatedRouteNetworkWalk(routeNetworkInterest.RouteNetworkElementRefs);
        }
   
        private async void NotifyExternalServicesAboutConnectivityChange(Guid spanEquipmentId, Guid routeNodeId, string category)
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
                    category: category,
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: new Guid[] { routeNodeId }
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }

        private async void NotifyExternalServicesAboutMerge(Guid spanEquipmentId, Guid[] affectedRouteNetworkElements)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Addition, new Guid[] { spanEquipmentId })
            };

            var updatedEvent =
                new RouteNetworkElementContainedEquipmentUpdated(
                    eventType: typeof(RouteNetworkElementContainedEquipmentUpdated).Name,
                    eventId: Guid.NewGuid(),
                    eventTimestamp: DateTime.UtcNow,
                    applicationName: "UtilityNetworkService",
                    applicationInfo: null,
                    category: "EquipmentModification.Merge",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: affectedRouteNetworkElements
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);

        }


        private class SpanEquipmentWithConnectsHolder
        {
            public SpanEquipment SpanEquipment { get; }
            public List<SpanSegmentConnectHolder> Connects { get; set; }

            public SpanEquipmentWithConnectsHolder(SpanEquipment spanEquipment)
            {
                SpanEquipment = spanEquipment;
                Connects = new();
            }
        }

        private class SpanSegmentConnectHolder
        {
            public SpanSegmentToSimpleTerminalConnectInfo ConnectInfo { get; }
            public Guid StructureSpecificationId { get; set; }
            public ushort StructureIndex { get; set; }
            public SpanSegmentConnectHolder(SpanSegmentToSimpleTerminalConnectInfo connectInfo)
            {
                ConnectInfo = connectInfo;
            }
        }
    }
}

  