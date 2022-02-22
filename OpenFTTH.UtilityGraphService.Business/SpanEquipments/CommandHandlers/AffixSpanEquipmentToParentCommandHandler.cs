﻿using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.Trace;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
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
            var spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            if (command.RouteNodeId == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_ROUTE_NODE_ID_CANNOT_BE_EMPTY, $"Round node id must be specified.")));

            if (command.ChildSpanSegmentId == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_CANNOT_BE_EMPTY, $"Span segment id 1 must be specified.")));

            if (command.ParentSpanSegmentId == Guid.Empty)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_CANNOT_BE_EMPTY, $"Span segment id 2 must be specified.")));


            // Find first span equipment
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.ChildSpanSegmentId, out var spanSegment1GraphElement))
            {
                if (_utilityNetwork.TryGetEquipment<SpanEquipment>(command.ChildSpanSegmentId, out var spanEquipment))
                {
                    if(!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanEquipment.SpanStructures[0].SpanSegments[0].Id, out spanSegment1GraphElement))
                         return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment with id: {command.ChildSpanSegmentId}")));
                }
                else
                    return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment with id: {command.ChildSpanSegmentId}")));
            }


            var spanEquipment1 = spanSegment1GraphElement.SpanEquipment(_utilityNetwork);

            // Find second span equipment
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(command.ParentSpanSegmentId, out var spanSegment2GraphElement))
            {
                if (_utilityNetwork.TryGetEquipment<SpanEquipment>(command.ParentSpanSegmentId, out var spanEquipment))
                {
                    if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanEquipment.SpanStructures[0].SpanSegments[0].Id, out spanSegment2GraphElement))
                        return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment with id: {command.ParentSpanSegmentId}")));
                }
                else
                    return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID_NOT_FOUND, $"Cannot find any span segment with id: {command.ParentSpanSegmentId}")));
            }

            var spanEquipment2 = spanSegment2GraphElement.SpanEquipment(_utilityNetwork);

            // Check that one of the equipments is a cable
            if (!spanEquipment1.IsCable && !spanEquipment2.IsCable)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.NO_CABLE_SPAN_SEGMENT_NOT_FOUND, $"One span segment must belong to a cable.")));

            // Check that one of the equipments is a conduit
            if (spanEquipment1.IsCable && spanEquipment2.IsCable)
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.NO_CONDUIT_SPAN_SEGMENT_NOT_FOUND, $"One span segment must belong to a conduit.")));

            var cableSpanEquipment = spanEquipment1.IsCable ? spanEquipment1 : spanEquipment2;
            var conduitSpanEquipment = spanEquipment1.IsCable ? spanEquipment2 : spanEquipment1;

            var conduitSpanSegmentId = spanEquipment1.IsCable ? command.ParentSpanSegmentId : command.ChildSpanSegmentId;
            var conduitSpanStructureIndex = spanEquipment1.IsCable ? spanSegment2GraphElement.StructureIndex : spanSegment1GraphElement.StructureIndex;

            // Check that not already contain cable, if single conduit
            var conduitSpec = spanEquipmentSpecifications[conduitSpanEquipment.SpecificationId];

            if (!conduitSpec.IsMultiLevel && _utilityNetwork.RelatedCablesByConduitSegmentId.ContainsKey(conduitSpanSegmentId))
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.NON_MULTI_LEVEL_CONDUIT_CANNOT_CONTAIN_MORE_THAN_ONE_CABLE, $"The cable with id {cableSpanEquipment.Id} cannot be affixed to conduit with id: {conduitSpanEquipment.Id} because cable already affixed to conduit and conduit is not a multi level conduit")));
              
            if (conduitSpanStructureIndex > 0 && _utilityNetwork.RelatedCablesByConduitSegmentId.ContainsKey(conduitSpanSegmentId))
                return Task.FromResult(Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.CONDUIT_SEGMENT_ALREADY_CONTAIN_CABLE, $"The cable with id {cableSpanEquipment.Id} cannot be affixed to conduit with id: {conduitSpanEquipment.Id} because cable already affixed to conduit segment: {conduitSpanSegmentId}")));

            var createAffixesResult = CreateHop(conduitSpanSegmentId);

            if (createAffixesResult.IsFailed)
                return Task.FromResult(Result.Fail(createAffixesResult.Errors.First()));

            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(cableSpanEquipment.Id);

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var existingWalkOfInterest = GetWalkOfInterest(cableSpanEquipment.WalkOfInterestId);

            var affixResult = spanEquipmentAR.AffixToParent(
                cmdContext: commandContext,
                childWalkOfInterest: existingWalkOfInterest,
                utilityNetworkHop: createAffixesResult.Value.Item1,
                utilityNetworkHopWalkOfInterest: createAffixesResult.Value.Item2.ValidatedRouteNetworkWalk
            );

            if (affixResult.IsFailed)
                return Task.FromResult(Result.Fail(affixResult.Errors.First()));

            // Check if walk of interest has changed
            var newWalkOfInterest = affixResult.Value;

            if (CheckIfWalkHasChanged(existingWalkOfInterest, newWalkOfInterest))
            {
                var updateWalkOfInterestCommand = new UpdateWalkOfInterest(commandContext.CorrelationId, commandContext.UserContext, cableSpanEquipment.WalkOfInterestId, newWalkOfInterest.RouteNetworkElementRefs);

                var updateWalkOfInterestCommandResult = _commandDispatcher.HandleAsync<UpdateWalkOfInterest, Result<RouteNetworkInterest>>(updateWalkOfInterestCommand).Result;

                if (updateWalkOfInterestCommandResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateWalkOfInterestCommandResult.Errors.First()));

                var moveSpanEquipmentResult = spanEquipmentAR.Move(commandContext, newWalkOfInterest, existingWalkOfInterest);

                if (moveSpanEquipmentResult.IsFailed)
                    return Task.FromResult(Result.Fail(moveSpanEquipmentResult.Errors.First()));

            }

            _eventStore.Aggregates.Store(spanEquipmentAR);

            NotifyExternalServicesAboutChange(cableSpanEquipment.Id, new Guid[] { command.RouteNodeId });

            return Task.FromResult(Result.Ok());
        }

        private bool CheckIfWalkHasChanged(ValidatedRouteNetworkWalk existingWalkOfInterest, ValidatedRouteNetworkWalk newWalkOfInterest)
        {
            if (existingWalkOfInterest.RouteNetworkElementRefs.Count != newWalkOfInterest.RouteNetworkElementRefs.Count)
                return true;

            for (int i = 0; i < existingWalkOfInterest.RouteNetworkElementRefs.Count; i++)
            {
                var existingRouteNetworkElement = existingWalkOfInterest.RouteNetworkElementRefs[i];
                var newRouteNetworkElement = newWalkOfInterest.RouteNetworkElementRefs[i];

                if (existingRouteNetworkElement != newRouteNetworkElement)
                    return true;
            }

            return false;
        }

        private Result<(UtilityNetworkHop, ProcessedHopResult)> CreateHop(Guid conduitSpanSegmentToTrace)
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

            return Result.Ok((new UtilityNetworkHop(segmentTrace.ValidatedRouteNetworkWalk.FromNodeId, segmentTrace.ValidatedRouteNetworkWalk.ToNodeId, affixes.ToArray()), segmentTraceResult.Value));
        }


        private Result<ProcessedHopResult> TraceSpanSegment(Guid spanSegmentIdToTrace)
        {
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanSegmentIdToTrace, out var spanSegmentGraphElement))
                return Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.INVALID_SPAN_SEGMENT_ID, $"Cannot find any span segment in the utility graph with id: {spanSegmentIdToTrace}"));

            var spanEquipment = spanSegmentGraphElement.SpanEquipment(_utilityNetwork);

            var traceBuilder = new ConduitSpanSegmentTracer(_queryDispatcher, _utilityNetwork);

            var traceInfo = traceBuilder.Trace(spanSegmentIdToTrace);

            if (traceInfo == null || traceInfo.RouteNetworkWalk == null)
            {
                return Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected route network trace result"));
            }

            if (traceInfo == null || traceInfo.UtilityNetworkTrace == null)
            {
                return Result.Fail(new AffixSpanEquipmentToParentError(AffixSpanEquipmentToParentErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected utility network trace result"));
            }
                       

            var walk = new RouteNetworkElementIdList();
            walk.AddRange(traceInfo.RouteNetworkWalk);

            var validateInterestCommand = new ValidateWalkOfInterest(Guid.NewGuid(), new UserContext("PlaceSpanEquipmentInRouteNetwork", Guid.Empty), walk);

            var validateInterestResult = _commandDispatcher.HandleAsync<ValidateWalkOfInterest, Result<ValidatedRouteNetworkWalk>>(validateInterestCommand).Result;

            if (validateInterestResult.IsFailed)
                return Result.Fail(validateInterestResult.Errors.First());

            return Result.Ok(
                new ProcessedHopResult(spanSegmentIdToTrace, validateInterestResult.Value, traceInfo.UtilityNetworkTrace)
            );
        }

        public ValidatedRouteNetworkWalk GetWalkOfInterest(Guid interestId)
        {
            var routeNetworkQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
                 new GetRouteNetworkDetails(new InterestIdList() { interestId })
                 {
                     RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementAndInterestObjects
                 }
            ).Result;

            if (routeNetworkQueryResult.IsFailed)
                throw new ApplicationException(routeNetworkQueryResult.Errors.First().Message);

            return new ValidatedRouteNetworkWalk(routeNetworkQueryResult.Value.Interests.First().RouteNetworkElementRefs);
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
            public UtilityNetworkTraceResult? UtilityNetworkTrace { get; }
            public Guid? SegmentId { get; }

            public ProcessedHopResult(Guid? segmentId, ValidatedRouteNetworkWalk validatedRouteNetworkWalk, UtilityNetworkTraceResult? utilityNetworkTrace)
            {
                SegmentId = segmentId;
                ValidatedRouteNetworkWalk = validatedRouteNetworkWalk;
                UtilityNetworkTrace = utilityNetworkTrace;
            }
        }
    }
}

  