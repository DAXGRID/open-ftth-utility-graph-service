﻿using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Projections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class PlaceRackInNodeContainerCommandHandler : ICommandHandler<PlaceRackInNodeContainer, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private readonly IExternalEventProducer _externalEventProducer;

        public PlaceRackInNodeContainerCommandHandler(IEventStore eventStore, IExternalEventProducer externalEventProducer)
        {
            _externalEventProducer = externalEventProducer;
            _eventStore = eventStore;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result> HandleAsync(PlaceRackInNodeContainer command)
        {
            if (!_utilityNetwork.TryGetEquipment<NodeContainer>(command.NodeContainerId, out var nodeContainer))
            {
                return Task.FromResult(Result.Fail(new NodeContainerError(NodeContainerErrorCodes.NODE_CONTAINER_NOT_FOUND, $"Cannot find any node container with id: {command.NodeContainerId}")));
            }

            var nodeContainerAR = _eventStore.Aggregates.Load<NodeContainerAR>(command.NodeContainerId);

            var rackSpecifications = _eventStore.Projections.Get<RackSpecificationsProjection>().Specifications;

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var placeRackResult = nodeContainerAR.PlaceRack(
                commandContext,
                command.RackId,
                command.RackSpecificationId,
                command.RackName,
                command.RackPosition,
                command.RackHeightInUnits,
                rackSpecifications
            );

            if (placeRackResult.IsSuccess)
            {
                _eventStore.Aggregates.Store(nodeContainerAR);
                NotifyExternalServicesAboutChange(nodeContainer.RouteNodeId, nodeContainer.Id);
            }

            return Task.FromResult(placeRackResult);
        }

        private async void NotifyExternalServicesAboutChange(Guid routeNodeId, Guid nodeContainerId)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("NodeContainer", ChangeTypeEnum.Addition, new Guid[] { nodeContainerId })
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
                    affectedRouteNetworkElementIds: new Guid[] { routeNodeId }
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }
    }
}

  