using DAX.EventProcessing;
using FluentResults;
using Newtonsoft.Json;
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
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class MoveSpanEquipmentCommandHandler : ICommandHandler<MoveSpanEquipment, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public MoveSpanEquipmentCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result> HandleAsync(MoveSpanEquipment command)
        {
            // Because the client is allowed to provide either a span equipment or segment id, we need look it up via the utility network graph
            if (!_utilityNetwork.TryGetEquipment<SpanEquipment>(command.SpanEquipmentOrSegmentId, out SpanEquipment spanEquipment))
                return Task.FromResult(Result.Fail(new MoveSpanEquipmentError(MoveSpanEquipmentErrorCodes.SPAN_EQUIPMENT_NOT_FOUND, $"Cannot find any span equipment or segment in the utility graph with id: {command.SpanEquipmentOrSegmentId}")));

            // Get interest information from existing span equipment
            var existingWalk = GetInterestInformation(spanEquipment);

            // Validate the new walk
            var newWalkValidationResult = _commandDispatcher.HandleAsync<ValidateWalkOfInterest, Result<ValidatedRouteNetworkWalk>>(new ValidateWalkOfInterest(Guid.NewGuid(), new UserContext("test", Guid.Empty), command.NewWalkIds)).Result;

            // If the new walk fails to validate, return the error to the client
            if (newWalkValidationResult.IsFailed)
                return Task.FromResult(Result.Fail(newWalkValidationResult.Errors.First()));

            var newWalk = newWalkValidationResult.Value;

            // If the walk has not changed return error as well
            if (existingWalk.Equals(newWalk))
                return Task.FromResult(Result.Fail(new MoveSpanEquipmentError(MoveSpanEquipmentErrorCodes.NEW_WALK_EQUALS_EXISTING_WALK, $"The new walk specified is not different from the existing walk of the span equipment.")));

            // Reverse new walk if one of its ends are opposite of existing walk ends
            if (newWalk.FromNodeId == existingWalk.ToNodeId || newWalk.ToNodeId == existingWalk.FromNodeId)
                newWalk = newWalk.Reverse();

            // Try to do the move of the span equipment
            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(spanEquipment.Id);

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var moveSpanEquipmentResult = spanEquipmentAR.Move(commandContext, newWalk, existingWalk);

            if (moveSpanEquipmentResult.IsFailed)
                return Task.FromResult(Result.Fail(moveSpanEquipmentResult.Errors.First()));


            // If span equipment contains cable, move these as well
            Dictionary<Guid, ValidatedRouteNetworkWalk> childWalkOfInterestsToUpdate = new();
            List<SpanEquipmentAR> childARsToStore = new();

            if (HasAnyChildSpanEquipments(spanEquipment))
            {
                // Check if end points are moved, which is not allowed when cables are running through conduit
                /*
                if (existingWalk.FromNodeId != newWalk.FromNodeId || existingWalk.ToNodeId != newWalk.ToNodeId)
                    return Task.FromResult(Result.Fail(new MoveSpanEquipmentError(MoveSpanEquipmentErrorCodes.ENDS_CANNOT_BE_MOVED_BECAUSE_OF_CHILD_SPAN_EQUIPMENTS, $"The ends of the walk of span equipment with id: { spanEquipment.Id } cannot be modified because of child equipments related to the span equipment")));
                */

                var children = GetChildSpanEquipments(spanEquipment);

                foreach (var child in children)
                {
                    var childSpanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(child.Id);

                    var existingChildWalk = GetInterestInformation(child);

                    var childMoveResult = childSpanEquipmentAR.MoveWithParent(commandContext, existingChildWalk, spanEquipment, newWalk, existingWalk);

                    if (childMoveResult.IsFailed)
                        return Task.FromResult(Result.Fail(childMoveResult.Errors.First()));

                    var newChildWalk = childMoveResult.Value;

                    if (!existingChildWalk.Equals(newChildWalk))
                    {
                        // Validate the new child walk
                        var newChildWalkValidationResult = _commandDispatcher.HandleAsync<ValidateWalkOfInterest, Result<ValidatedRouteNetworkWalk>>(new ValidateWalkOfInterest(Guid.NewGuid(), new UserContext("test", Guid.Empty), childMoveResult.Value.RouteNetworkElementRefs)).Result;

                        // If the new walk fails to validate, return the error to the client
                        if (newChildWalkValidationResult.IsFailed)
                            return Task.FromResult(Result.Fail(newChildWalkValidationResult.Errors.First()));

                        childWalkOfInterestsToUpdate.Add(child.WalkOfInterestId, newChildWalk);
                    }

                    childARsToStore.Add(childSpanEquipmentAR);
                }
            }

            // If we got to here, then the span equipment move was validated fine, so we can update the walk of interest
            var newSegmentIds = new RouteNetworkElementIdList();
            newSegmentIds.AddRange(newWalk.SegmentIds);

            var updateWalkOfInterestCommand = new UpdateWalkOfInterest(commandContext.CorrelationId, commandContext.UserContext, spanEquipment.WalkOfInterestId, newSegmentIds);

            var updateWalkOfInterestCommandResult = _commandDispatcher.HandleAsync<UpdateWalkOfInterest, Result<RouteNetworkInterest>>(updateWalkOfInterestCommand).Result;

            if (updateWalkOfInterestCommandResult.IsFailed)
                throw new ApplicationException($"Got unexpected error result: {updateWalkOfInterestCommandResult.Errors.First().Message} trying to update walk of interest belonging to span equipment with id: {spanEquipment.Id} while processing the MoveSpanEquipment command: " + JsonConvert.SerializeObject(command));

            // Update eventually child walk of interests
            foreach (var childWalkOfInterestToUpdate in childWalkOfInterestsToUpdate)
            {
                var updateChildWalkOfInterestCommand = new UpdateWalkOfInterest(commandContext.CorrelationId, commandContext.UserContext, childWalkOfInterestToUpdate.Key, childWalkOfInterestToUpdate.Value.RouteNetworkElementRefs);

                var updateChildWalkOfInterestCommandResult = _commandDispatcher.HandleAsync<UpdateWalkOfInterest, Result<RouteNetworkInterest>>(updateChildWalkOfInterestCommand).Result;

                if (updateChildWalkOfInterestCommandResult.IsFailed)
                    throw new ApplicationException($"Got unexpected error result: {updateWalkOfInterestCommandResult.Errors.First().Message} trying to update child walk of interest: {childWalkOfInterestToUpdate.Key} while processing the MoveSpanEquipment command: " + JsonConvert.SerializeObject(command));
            }

            // Store child aggregates
            foreach (var childAR in childARsToStore)
            {
                _eventStore.Aggregates.Store(childAR);
            }


            // Store the aggregate
            _eventStore.Aggregates.Store(spanEquipmentAR);

            NotifyExternalServicesAboutSpanEquipmentChange(spanEquipment.Id, existingWalk, newWalk);

            return Task.FromResult(moveSpanEquipmentResult);
        }

        private bool HasAnyChildSpanEquipments(SpanEquipment spanEquipment)
        {
            foreach (var spanStructure in spanEquipment.SpanStructures)
            {
                foreach (var spanSegment in spanStructure.SpanSegments)
                {
                    if (_utilityNetwork.RelatedCablesByConduitSegmentId.ContainsKey(spanSegment.Id))
                        return true;
                }
            }

            return false;
        }

        private List<SpanEquipment> GetChildSpanEquipments(SpanEquipment spanEquipment)
        {
            List<SpanEquipment> result = new();

            foreach (var spanStructure in spanEquipment.SpanStructures)
            {
                foreach (var spanSegment in spanStructure.SpanSegments)
                {
                    if (_utilityNetwork.RelatedCablesByConduitSegmentId.ContainsKey(spanSegment.Id))
                    {
                        var childIds = _utilityNetwork.RelatedCablesByConduitSegmentId[spanSegment.Id];

                        foreach (var childId in childIds)
                        {
                            if (_utilityNetwork.TryGetEquipment<SpanEquipment>(childId, out var child))
                            {
                                result.Add(child);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private bool IsChildOfSpanEquipments(SpanEquipment spanEquipment)
        {
            if (spanEquipment.UtilityNetworkHops != null && spanEquipment.UtilityNetworkHops.Count() > 0)
                return true;
            else
                return false;
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

        private async void NotifyExternalServicesAboutSpanEquipmentChange(Guid spanEquipmentId, ValidatedRouteNetworkWalk existingWalk, ValidatedRouteNetworkWalk newWalk)
        {
            var routeIdsAffected = new RouteNetworkElementIdList();

            foreach (var id in existingWalk.RouteNetworkElementRefs)
                routeIdsAffected.Add(id);

            foreach (var id in newWalk.RouteNetworkElementRefs)
            {
                if (!routeIdsAffected.Contains(id))
                    routeIdsAffected.Add(id);
            }

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
                    category: "EquipmentModification.Moved",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: routeIdsAffected.ToArray()
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }

    }
}

  