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
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class PlaceSpanEquipmentInUtilityNetworkCommandHandler : ICommandHandler<PlaceSpanEquipmentInUtilityNetwork, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public PlaceSpanEquipmentInUtilityNetworkCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result> HandleAsync(PlaceSpanEquipmentInUtilityNetwork command)
        {
            if (command.RoutingHops == null || command.RoutingHops.Length == 0)
            {
                return Task.FromResult(Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.ROUTING_HOPS_CANNOT_BE_NULL_OR_EMPTY, $"One or more routing hops must be specified")));
            }

            var traceRoutingHopsResult = ProcessRoutingHops(command);

            if (traceRoutingHopsResult.IsFailed)
                return Task.FromResult(Result.Fail(traceRoutingHopsResult.Errors.First()));

            if (traceRoutingHopsResult.Value.ValidatedRouteNetworkWalk == null)
                throw new ApplicationException("ProcessRoutingHops return IsSuccess and null value. Please check code.");

            var walkOfInterestId = Guid.NewGuid();

            var spanEquipments = _eventStore.Projections.Get<UtilityNetworkProjection>().SpanEquipments;
            var spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            var spanEquipmentAR = new SpanEquipmentAR();

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var placeSpanEquipmentResult = spanEquipmentAR.PlaceSpanEquipmentInUtilityNetwork(
                cmdContext: commandContext,
                spanEquipments, 
                spanEquipmentSpecifications, 
                command.SpanEquipmentId,
                command.SpanEquipmentSpecificationId,
                walkOfInterestId,
                traceRoutingHopsResult.Value.ValidatedRouteNetworkWalk.RouteNetworkElementRefs,
                traceRoutingHopsResult.Value.SpanEquipmentSpanEquipmentAffixes.ToArray(),
                command.ManufacturerId,
                command.NamingInfo,
                command.LifecycleInfo,
                command.MarkingInfo,
                command.AddressInfo
            );

            if (placeSpanEquipmentResult.IsFailed)
                return Task.FromResult(Result.Fail(placeSpanEquipmentResult.Errors.First()));

            // If we got to here, then the span equipment placement was validated fine, so we can register the walk of interest
            var registerWalkOfInterestCommand = new RegisterWalkOfInterest(commandContext.CorrelationId, commandContext.UserContext, walkOfInterestId, traceRoutingHopsResult.Value.ValidatedRouteNetworkWalk.RouteNetworkElementRefs);

            var registerWalkOfInterestCommandResult = _commandDispatcher.HandleAsync<RegisterWalkOfInterest, Result<RouteNetworkInterest>>(registerWalkOfInterestCommand).Result;

            if (registerWalkOfInterestCommandResult.IsFailed)
                return Task.FromResult(Result.Fail(registerWalkOfInterestCommandResult.Errors.First()));

            _eventStore.Aggregates.Store(spanEquipmentAR);

            NotifyExternalServicesAboutChange(command.SpanEquipmentId, traceRoutingHopsResult.Value.ValidatedRouteNetworkWalk.RouteNetworkElementRefs.ToArray());

            return Task.FromResult(Result.Ok());
        }

        private Result<ProcessRoutingHopsResult> ProcessRoutingHops(PlaceSpanEquipmentInUtilityNetwork command)
        {
            // Trace all hops
            var traceAllHopsResult = TraceAllHops(command);

            if (traceAllHopsResult.IsFailed)
                return Result.Fail(traceAllHopsResult.Errors.First());

            var segmentTraceResults = traceAllHopsResult.Value;


            // Make sure walks can be connected together
            var checkAndReverseSpanSegmentTracesResult = CheckAndReverseSpanSegmentTraces(segmentTraceResults);

            if (checkAndReverseSpanSegmentTracesResult.IsFailed)
                return Result.Fail(checkAndReverseSpanSegmentTracesResult.Errors.First());


            // Connect walks together
            RouteNetworkElementIdList routeNeworkElements = new RouteNetworkElementIdList();

            routeNeworkElements.AddRange(segmentTraceResults[0].ValidatedRouteNetworkWalk.RouteNetworkElementRefs);

            for (int subWalkIndex = 1; subWalkIndex < segmentTraceResults.Count; subWalkIndex++)
            {
                for (int routeNetworkElementIndex = 1; routeNetworkElementIndex < segmentTraceResults[subWalkIndex].ValidatedRouteNetworkWalk.RouteNetworkElementRefs.Count; routeNetworkElementIndex++)
                {
                    routeNeworkElements.Add(segmentTraceResults[subWalkIndex].ValidatedRouteNetworkWalk.RouteNetworkElementRefs[routeNetworkElementIndex]);
                }
            }

            // Create span equipment parent affixes
            List<SpanEquipmentSpanEquipmentAffix> affixes = new();

            foreach (var segmentTrace in segmentTraceResults)
            {
                if (segmentTrace.UtilityNetworkTrace != null)
                {
                    foreach (var segmentId in segmentTrace.UtilityNetworkTrace.SpanSegmentIds)
                    {
                        SpanEquipmentAffixDirectionEnum direction = segmentTrace.IsReversed ? SpanEquipmentAffixDirectionEnum.Backward : SpanEquipmentAffixDirectionEnum.Forward;

                        affixes.Add(new SpanEquipmentSpanEquipmentAffix(segmentId, direction));
                    }
                }
            }

            return Result.Ok(
                new ProcessRoutingHopsResult(new ValidatedRouteNetworkWalk(routeNeworkElements), affixes)
            );
        }

        private Result CheckAndReverseSpanSegmentTraces(List<ProcessedHopResult> segmentTraceResults)
        {
            bool first = true;
            ValidatedRouteNetworkWalk? prevSubWalk = null;
            int hopNumber = 1;

            for (int i = 0; i < segmentTraceResults.Count; i++)
            {
                var currentSubWalk = segmentTraceResults[i].ValidatedRouteNetworkWalk;

                if (!first)
                {
                    if (prevSubWalk == null)
                        throw new ApplicationException("Expected prebSubWalk to be non-null. Please check code");

                    if (currentSubWalk.FromNodeId == prevSubWalk.ToNodeId)
                    {
                        // Everything perfect, we need not to reverse any sub walks
                    }
                    else if (currentSubWalk.ToNodeId == prevSubWalk.ToNodeId)
                    {
                        // We reverse the current one
                        segmentTraceResults[i].ValidatedRouteNetworkWalk = currentSubWalk.Reverse();
                        segmentTraceResults[i].IsReversed = true;
                    }
                    else if (currentSubWalk.FromNodeId == prevSubWalk.FromNodeId)
                    {
                        // We reverse the prev one
                        segmentTraceResults[i - 1].ValidatedRouteNetworkWalk = prevSubWalk.Reverse();
                        segmentTraceResults[i - 1].IsReversed = true;
                    }
                    else if (currentSubWalk.ToNodeId == prevSubWalk.FromNodeId)
                    {
                        // We reverse both
                        segmentTraceResults[i].ValidatedRouteNetworkWalk = currentSubWalk.Reverse();
                        segmentTraceResults[i].IsReversed = true;

                        segmentTraceResults[i - 1].ValidatedRouteNetworkWalk = prevSubWalk.Reverse();
                        segmentTraceResults[i - 1].IsReversed = true;
                    }
                    else
                    {
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.ERROR_CALCULATING_WALK, $"Cannot connect walk of hop number: {hopNumber - 1} ({prevSubWalk.FromNodeId}->{prevSubWalk.ToNodeId}) to walk of hop number: {hopNumber} ({currentSubWalk.FromNodeId}->{currentSubWalk.ToNodeId}). Are you sure the walks/traces of the routing hops specified are adjacent?"));
                    }
                }

                first = false;
                prevSubWalk = currentSubWalk;
                hopNumber++;
            }

            return Result.Ok();
        }

        private Result<List<ProcessedHopResult>> TraceAllHops(PlaceSpanEquipmentInUtilityNetwork command)
        {
            List<ProcessedHopResult> processedHopsResult = new();

            // Find walks for all hops
            foreach (var routingHop in command.RoutingHops)
            {
                // Route by span segment id
                if (routingHop.Kind == RoutingHopKind.RouteThroughSpanEquipmentBySpanSegmentId)
                {
                    if (routingHop.StartSpanSegmentId == null)
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_ROUTE_NETWORK_HOP, $"RoutingHopKind.RouteThroughSpanEquipmentBySpanSegmentId hop must have a non-null StartSpanSegmentId"));

                    var spanSegmentId = routingHop.StartSpanSegmentId.Value;

                    var tracedHopResult = TraceSpanSegment(spanSegmentId);

                    if (tracedHopResult.IsFailed)
                        return Result.Fail(tracedHopResult.Errors.First());

                    if (tracedHopResult.Value.ValidatedRouteNetworkWalk.FromNodeId != routingHop.StartRouteNode && tracedHopResult.Value.ValidatedRouteNetworkWalk.ToNodeId != routingHop.StartRouteNode)
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.SPAN_SEGMENT_NOT_RELATED_TO_ROUTE_NODE, $"The span segment: {tracedHopResult.Value.SegmentId} do not start or end in route node: {routingHop.StartRouteNode}"));

                    processedHopsResult.Add(tracedHopResult.Value);
                }
                // Route by span equipment id and structure index
                else if (routingHop.Kind == RoutingHopKind.RouteThroughSpanEquipmentByStructureIndex)
                {
                    if (routingHop.StartRouteNode == null)
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_ROUTE_NETWORK_HOP, $"RoutingHopKind.RouteThroughSpanEquipmentByStructureIndex hop must have a non-null StartRouteNode"));

                    if (routingHop.StartSpanEquipmentId == null)
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_ROUTE_NETWORK_HOP, $"RoutingHopKind.RouteThroughSpanEquipmentByStructureIndex hop must have a non-null StartSpanEquipmentId"));

                    if (routingHop.StartStrutureIndex == null)
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_ROUTE_NETWORK_HOP, $"RoutingHopKind.RouteThroughSpanEquipmentByStructureIndex hop must have a non-null StartStrutureIndex"));

                    // Try find the span segment id from provided hop info
                    var findSegmentIdResult = FindSpanSegmentId(routingHop.StartRouteNode.Value, routingHop.StartSpanEquipmentId.Value, routingHop.StartStrutureIndex.Value);

                    if (findSegmentIdResult.IsFailed)
                        return Result.Fail(findSegmentIdResult.Errors.First());

                    // Trace the hop
                    var tracedHopResult = TraceSpanSegment(findSegmentIdResult.Value);

                    if (tracedHopResult.IsFailed)
                        return Result.Fail(tracedHopResult.Errors.First());

                    processedHopsResult.Add(tracedHopResult.Value);
                }

                // Route through route network
                else if (routingHop.Kind == RoutingHopKind.RouteThroughRouteNetwork)
                {
                    if (routingHop.WalkOfinterest == null)
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_ROUTE_NETWORK_HOP, $"RoutingHopKind.RouteThroughRouteNetwork hop must have a non-null WalkOfInterest"));

                    var walk = new RouteNetworkElementIdList();
                    walk.AddRange(routingHop.WalkOfinterest);

                    processedHopsResult.Add(new ProcessedHopResult(null, new ValidatedRouteNetworkWalk(walk), null));
                }
            }

            return Result.Ok(processedHopsResult);
        }

        private Result<Guid> FindSpanSegmentId(Guid routeNodeId, Guid spanEquipmentId, int structureIndex)
        {
            if (!_utilityNetwork.TryGetEquipment<SpanEquipment>(spanEquipmentId, out var spanEquipment))
                return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_SPAN_EQUIPMENT_ID_NOT_FOUND, $"Cannot find any span equipment in the utility graph with id: {spanEquipmentId}"));

            if (structureIndex < 0 || structureIndex >= spanEquipment.SpanStructures.Length)
                return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_SPAN_EQUIPMENT_STRUCTURE_INDEX_OUT_OF_BOUNDS, $"Cannot lookup structure in span equipment {spanEquipmentId} at index: {structureIndex}. The span equipment has {spanEquipment.SpanStructures.Length} structures."));

            var spanStructure = spanEquipment.SpanStructures[structureIndex];

            foreach (var spanSegment in spanStructure.SpanSegments)
            {
                var fromNode = spanEquipment.NodesOfInterestIds[spanSegment.FromNodeOfInterestIndex];

                if (fromNode == routeNodeId)
                    return Result.Ok(spanSegment.Id);

                var toNode = spanEquipment.NodesOfInterestIds[spanSegment.ToNodeOfInterestIndex];

                if (toNode == routeNodeId)
                    return Result.Ok(spanSegment.Id);
            }

            return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.SPAN_SEGMENT_NOT_RELATED_TO_ROUTE_NODE, $"The span segment: {spanEquipment} in span equipment {spanEquipmentId} do not start or end in route node: {routeNodeId}"));
        }

        private Result<ProcessedHopResult> TraceSpanSegment(Guid spanSegmentIdToTrace)
        {
            if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanSegmentIdToTrace, out var spanSegmentGraphElement))
                return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_SPAN_SEGMENT_ID, $"Cannot find any span segment in the utility graph with id: {spanSegmentIdToTrace}"));

            var spanEquipment = spanSegmentGraphElement.SpanEquipment(_utilityNetwork);

            var traceBuilder = new RouteNetworkTraceHelper(_queryDispatcher, _utilityNetwork);

            var traceInfo = traceBuilder.GetTraceInfo(new List<SpanEquipment> { spanEquipment }, spanSegmentIdToTrace);

            if (traceInfo == null || traceInfo.RouteNetworkTraces.Count != 1)
            {
                return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected 1 route network trace result, got {traceInfo?.RouteNetworkTraces?.Count}"));
            }

            if (traceInfo == null || traceInfo.UtilityNetworkTraceBySpanSegmentId.Count != 1)
            {
                return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected 1 utility network trace result, got {traceInfo?.UtilityNetworkTraceBySpanSegmentId?.Count}"));
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
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Addition, new Guid[] { spanEquipmentId })
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


        class ProcessRoutingHopsResult
        {
            public ValidatedRouteNetworkWalk ValidatedRouteNetworkWalk { get; set; }

            public List<SpanEquipmentSpanEquipmentAffix> SpanEquipmentSpanEquipmentAffixes { get; set; }

            public ProcessRoutingHopsResult(ValidatedRouteNetworkWalk validatedRouteNetworkWalk, List<SpanEquipmentSpanEquipmentAffix> spanEquipmentSpanEquipmentAffixes)
            {
                ValidatedRouteNetworkWalk = validatedRouteNetworkWalk;
                this.SpanEquipmentSpanEquipmentAffixes = spanEquipmentSpanEquipmentAffixes;
            }
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

  