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
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class UpdateSpanEquipmentPropertiesCommandHandler : ICommandHandler<UpdateSpanEquipmentProperties, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IExternalEventProducer _externalEventProducer;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;

        public UpdateSpanEquipmentPropertiesCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
        }

        public Task<Result> HandleAsync(UpdateSpanEquipmentProperties command)
        {
            var spanEquipments = _eventStore.Projections.Get<UtilityNetworkProjection>().SpanEquipments;
            var spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            // Because the client is allowed to provide either a span equipment or segment id, we need look it up via the utility network graph
            if (!utilityNetwork.TryGetEquipment<SpanEquipment>(command.SpanEquipmentOrSegmentId, out SpanEquipment spanEquipment))
                return Task.FromResult(Result.Fail(new MoveSpanEquipmentError(MoveSpanEquipmentErrorCodes.SPAN_EQUIPMENT_NOT_FOUND, $"Cannot find any span equipment or segment in the utility graph with id: {command.SpanEquipmentOrSegmentId}")));

            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(command.SpanEquipmentOrSegmentId);

            // Get interest information for span equipment
            var walk = GetInterestInformation(spanEquipment);

            bool somethingChanged = false; 

            if (command.MarkingInfo != null && !command.MarkingInfo.Equals(spanEquipment.MarkingInfo))
            {
                var updateMarkingInfoResult = spanEquipmentAR.UpdateMarkingInfo(
                    command.MarkingInfo
                );

                if (updateMarkingInfoResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateMarkingInfoResult.Errors.First()));

                somethingChanged = true;
            }

            if (somethingChanged)
            {
                _eventStore.Aggregates.Store(spanEquipmentAR);

                NotifyExternalServicesAboutSpanEquipmentChange(spanEquipment.Id, walk);

                return Task.FromResult(Result.Ok());
            }
            else
            {
                return Task.FromResult(Result.Fail(new UpdateSpanEquipmentPropertiesError(
                      UpdateSpanEquipmentPropertiesErrorCodes.NO_CHANGE,
                      $"Will not update span equipment, because no difference found in provided arguments compared to the current values of the span equpment.")
                  ));
            }
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

        private async void NotifyExternalServicesAboutSpanEquipmentChange(Guid spanEquipmentId, ValidatedRouteNetworkWalk walk)
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
                    category: "EquipmentModification.PropertiesUpdated",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: walk.RouteNetworkElementRefs.ToArray()
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }
    }
}

  