using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class AffixSpanEquipmentToParentCommandHandler : ICommandHandler<AffixSpanEquipmentToParent, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public AffixSpanEquipmentToParentCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result> HandleAsync(AffixSpanEquipmentToParent command)
        {
            if (command.RouteNodeId == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_ROUTE_NODE_ID_CANNOT_BE_EMPTY, $"Round node id must be specified.")));

            if (command.SpanSegmentId1 == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_CANNOT_BE_EMPTY, $"Span segment id 1 must be specified.")));

            if (command.SpanSegmentId2 == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_CANNOT_BE_EMPTY, $"Span segment id 2 must be specified.")));

          
            // Find first span equipment
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.SpanSegmentId1, out var spanSegment1GraphElement))
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentError.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment with id: {command.SpanSegmentId1}")));

            var spanEquipment1 = spanSegment1GraphElement.SpanEquipment(_utilityNetwork);

            // Find second span equipment
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.SpanSegmentId2, out var spanSegment2GraphElement))
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentError.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment with id: {command.SpanSegmentId2}")));

            var spanEquipment2 = spanSegment2GraphElement.SpanEquipment(_utilityNetwork);

            // Check that one of the equipments is a cable
            if (!spanEquipment1.IsCable && !spanEquipment2.IsCable)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentError.NO_CABLE_SPAN_SEGMENT_NOT_FOUND, $"One span segment must belong to a cable.")));

            // Check that one of the equipments is a conduit
            if (spanEquipment1.IsCable && spanEquipment2.IsCable)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentError.NO_CONDUIT_SPAN_SEGMENT_NOT_FOUND, $"One span segment must belong to a conduit.")));

            var cableSpanEquipment = spanEquipment1.IsCable ? spanEquipment1 : spanEquipment2;
            var conduitSpanSegmentId = spanEquipment1.IsCable ? command.SpanSegmentId2 : command.SpanSegmentId1;

            var createAffixesResult = CreateSpanEquipmentAffixes(conduitSpanSegmentId);

            if (createAffixesResult.IsFailed)
                return Task.FromResult(Result.Fail(createAffixesResult.Errors.First()));

            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(cableSpanEquipment.Id);

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var affixResult = spanEquipmentAR.AffixToParent(
                cmdContext: commandContext,
                spanEquipmentAffixes: createAffixesResult.Value.ToArray()
            );

            if (affixResult.IsSuccess)
            {
                _eventStore.Aggregates.Store(spanEquipmentAR);

                NotifyExternalServicesAboutChange(cableSpanEquipment.Id, new Guid[] { command.RouteNodeId });
            }

            return Task.FromResult(affixResult);
        }


        private Result<List<SpanEquipmentSpanEquipmentAffix>> CreateSpanEquipmentAffixes(Guid conduitSpanSegmentToTrace)
        {
            // Create span equipment parent affixes
            List<SpanEquipmentSpanEquipmentAffix> affixes = new();

            var segmentTraceResult = TraceSpanSegment(conduitSpanSegmentToTrace);

            if (segmentTraceResult.IsFailed)
                return Result.Fail(segmentTraceResult.Errors.First());

            var segmentTrace = segmentTraceResult.Value;

            if (segmentTrace.UtilityNetworkTrace != null)
            {
                foreach (var segmentId in segmentTrace.UtilityNetworkTrace.SpanSegmentIds)
                {
                    SpanEquipmentAffixDirectionEnum direction = segmentTrace.IsReversed ? SpanEquipmentAffixDirectionEnum.Backward : SpanEquipmentAffixDirectionEnum.Forward;

                    affixes.Add(new SpanEquipmentSpanEquipmentAffix(segmentId, direction));
                }
            }

            return Result.Ok(affixes);
        }


        private Result<ProcessedHopResult> TraceSpanSegment(Guid spanSegmentIdToTrace)
        {
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanSegmentIdToTrace, out var spanSegmentGraphElement))
                return Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID, $"Cannot find any span segment in the utility graph with id: {spanSegmentIdToTrace}"));

            var spanEquipment = spanSegmentGraphElement.SpanEquipment(_utilityNetwork);

            var traceBuilder = new RouteNetworkTraceHelper(_queryDispatcher, _utilityNetwork);

            var traceInfo = traceBuilder.GetTraceInfo(new List<SpanEquipment> { spanEquipment }, spanSegmentIdToTrace);

            if (traceInfo == null || traceInfo.RouteNetworkTraces.Count != 1)
            {
                return Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected 1 route network trace result, got {traceInfo?.RouteNetworkTraces?.Count}"));
            }

            if (traceInfo == null || traceInfo.UtilityNetworkTraceBySpanSegmentId.Count != 1)
            {
                return Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected 1 utility network trace result, got {traceInfo?.UtilityNetworkTraceBySpanSegmentId?.Count}"));
            }

            var walk = new RouteNetworkElementIdList();
            walk.AddRange(traceInfo.RouteNetworkTraces[0].RouteSegmentIds);

            var validateInterestCommand = new ValidateWalkOfInterest(Guid.NewGuid(), new UserContext("PlaceSpanEquipmentInRouteNetwork", Guid.Empty), walk);

            var validateInterestResult = _commandDispatcher.HandleAsync<ValidateWalkOfInterest, Result<ValidatedRouteNetworkWalk>>(validateInterestCommand).Result;

            if (validateInterestResult.IsFailed)
                return Result.Fail(validateInterestResult.Errors.First());

            return Result.Ok(
                new ProcessedHopResult(spanSegmentIdToTrace, validateInterestResult.Value, traceInfo.UtilityNetworkTraceBySpanSegmentId.Values.First())
            );
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

        class ProcessedHopResult
        {
            public ValidatedRouteNetworkWalk ValidatedRouteNetworkWalk { get; set; }
            public bool IsReversed { get; set; }
            public UtilityNetworkTrace? UtilityNetworkTrace { get; }
            public Guid? SegmentId { get; }

            public ProcessedHopResult(Guid? segmentId, ValidatedRouteNetworkWalk validatedRouteNetworkWalk, UtilityNetworkTrace? utilityNetworkTrace)
            {
                SegmentId = segmentId;
                ValidatedRouteNetworkWalk = validatedRouteNetworkWalk;
                UtilityNetworkTrace = utilityNetworkTrace;
            }
        }
    }
}

  