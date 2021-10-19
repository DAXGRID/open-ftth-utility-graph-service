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
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.QueryHandlers.Trace;
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

            var walkOfInterest = CalculateWalkOfInterest(command);
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
                walkOfInterest.Value.RouteNetworkElementRefs,
                command.RoutingHops,
                command.ManufacturerId,
                command.NamingInfo,
                command.LifecycleInfo,
                command.MarkingInfo,
                command.AddressInfo
            );

            if (placeSpanEquipmentResult.IsFailed)
                return Task.FromResult(Result.Fail(placeSpanEquipmentResult.Errors.First()));

            // If we got to here, then the span equipment placement was validated fine, so we can register the walk of interest
            var registerWalkOfInterestCommand = new RegisterWalkOfInterest(commandContext.CorrelationId, commandContext.UserContext, walkOfInterestId, walkOfInterest.Value.RouteNetworkElementRefs);

            var registerWalkOfInterestCommandResult = _commandDispatcher.HandleAsync<RegisterWalkOfInterest, Result<RouteNetworkInterest>>(registerWalkOfInterestCommand).Result;

            if (registerWalkOfInterestCommandResult.IsFailed)
                return Task.FromResult(Result.Fail(placeSpanEquipmentResult.Errors.First()));

            _eventStore.Aggregates.Store(spanEquipmentAR);

            NotifyExternalServicesAboutChange(command.SpanEquipmentId, walkOfInterest.Value.RouteNetworkElementRefs.ToArray());

            return Task.FromResult(Result.Ok());
        }

        private Result<ValidatedRouteNetworkWalk> CalculateWalkOfInterest(PlaceSpanEquipmentInUtilityNetwork command)
        {
            List<ValidatedRouteNetworkWalk> validatedSubWalks = new();

            // Find walks for all hops
            foreach (var routingHop in command.RoutingHops)
            {
                if (routingHop.Kind == RoutingHopKind.RouteThroughSpanEquipmentBySpanSegmentId && routingHop.StartSpanSegmentId != null)
                {
                    var spanSegmentId = routingHop.StartSpanSegmentId.Value;

                    if (!_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanSegmentId, out var spanSegmentGraphElement))
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.INVALID_SPAN_SEGMENT_ID, $"Cannot find any span segment in the utility graph with id: {spanSegmentId}"));

                    var validateWalkResult = GetWalkFromSpanSegmentTrace(spanSegmentGraphElement.SpanEquipment(_utilityNetwork), spanSegmentId);

                    if (validateWalkResult.IsFailed)
                        return Result.Fail(validateWalkResult.Errors.First());

                    validatedSubWalks.Add(validateWalkResult.Value);
                }
            }

            // Make sure walks can be connected together
            bool first = true;
            ValidatedRouteNetworkWalk? prevSubWalk = null;
            int hopNumber = 1;

            foreach (var currentSubWalk in validatedSubWalks)
            {
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
                        currentSubWalk.Reverse();
                    }
                    else if (currentSubWalk.FromNodeId == prevSubWalk.FromNodeId)
                    {
                        // We reverse the prev one
                        prevSubWalk.Reverse();
                    }
                    else if (currentSubWalk.ToNodeId == prevSubWalk.FromNodeId)
                    {
                        // We reverse both
                        currentSubWalk.Reverse();
                        prevSubWalk.Reverse();
                    }
                    else
                    {
                        return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.ERROR_CALCULATING_WALK, $"Cannot connect walk of hop number: {hopNumber - 1} to walk of hop number: {hopNumber}. Are you sure the walks/traces of the routing hops specified are adjacent?"));
                    }
                }

                first = false;
                prevSubWalk = currentSubWalk;
                hopNumber++;
            }

            // Connect walks together

            RouteNetworkElementIdList routeNeworkElements = new RouteNetworkElementIdList();

            routeNeworkElements.AddRange(validatedSubWalks[0].RouteNetworkElementRefs);

            for (int subWalkIndex = 1; subWalkIndex < validatedSubWalks.Count; subWalkIndex++)
            {
                for (int routeNetworkElementIndex = 1; routeNetworkElementIndex < validatedSubWalks[subWalkIndex].RouteNetworkElementRefs.Count; routeNetworkElementIndex++)
                {
                    routeNeworkElements.Add(validatedSubWalks[subWalkIndex].RouteNetworkElementRefs[routeNetworkElementIndex]);
                }
            }

            return Result.Ok(new ValidatedRouteNetworkWalk(routeNeworkElements));
        }

        private Result<ValidatedRouteNetworkWalk> GetWalkFromSpanSegmentTrace(SpanEquipment spanEquipment, Guid spanSegmentIdToTrace)
        {
            var traceBuilder = new RouteNetworkTraceResultBuilder(_queryDispatcher, _utilityNetwork);

            var traceInfo = traceBuilder.GetTraceInfo(new List<SpanEquipment> { spanEquipment }, spanSegmentIdToTrace);

            if (traceInfo == null || traceInfo.RouteNetworkTraces.Count != 1)
            {
                return Result.Fail(new PlaceSpanEquipmentInUtilityNetworkError(PlaceSpanEquipmentInUtilityNetworkErrorCodes.ERROR_TRACING_SPAN_SEGMENT, $"Error tracing span segment with id: {spanSegmentIdToTrace} in span equipment with id: {spanEquipment.Id}. Expected 1 trace result, got {traceInfo?.RouteNetworkTraces?.Count}"));
            }

            var walk = new RouteNetworkElementIdList();
            walk.AddRange(traceInfo.RouteNetworkTraces[0].RouteSegmentIds);

            var validateInterestCommand = new ValidateWalkOfInterest(Guid.NewGuid(), new UserContext("PlaceSpanEquipmentInRouteNetwork", Guid.Empty), walk);

            return _commandDispatcher.HandleAsync<ValidateWalkOfInterest, Result<ValidatedRouteNetworkWalk>>(validateInterestCommand).Result;
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
    }
}

  