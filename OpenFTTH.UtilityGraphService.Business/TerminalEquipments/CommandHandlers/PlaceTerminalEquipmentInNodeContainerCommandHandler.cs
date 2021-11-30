using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Projections;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class PlaceTerminalEquipmentInNodeContainerCommandHandler : ICommandHandler<PlaceTerminalEquipmentInNodeContainer, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IExternalEventProducer _externalEventProducer;

        public PlaceTerminalEquipmentInNodeContainerCommandHandler(IEventStore eventStore, IExternalEventProducer externalEventProducer)
        {
            _externalEventProducer = externalEventProducer;
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(PlaceTerminalEquipmentInNodeContainer command)
        {
            var nodeContainers = _eventStore.Projections.Get<UtilityNetworkProjection>().NodeContainers;

            if (!nodeContainers.TryGetValue(command.NodeContainerId, out var nodeContainer))
            {
                return Task.FromResult(Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.NODE_CONTAINER_NOT_FOUND, $"Cannot find any node container with id: {command.NodeContainerId}")));
            }

            // Create the terminal equipment
            var terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
            var terminalStructureSpecifications = _eventStore.Projections.Get<TerminalStructureSpecificationsProjection>().Specifications;

            var terminalEquipmentAR = new TerminalEquipmentAR();

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            var placeTerminalEquipmentResult = terminalEquipmentAR.Place(
                commandContext,
                terminalEquipmentSpecifications,
                terminalStructureSpecifications, 
                command.NodeContainerId,
                command.TerminalEquipmentId,
                command.TerminalEquipmentSpecificationId,
                command.NamingInfo,
                command.LifecycleInfo,
                command.ManufacturerId
            );

            if (placeTerminalEquipmentResult.IsFailed)
                return Task.FromResult(placeTerminalEquipmentResult);


            // Add terminal equipment to node container
            var nodeContainerAR = _eventStore.Aggregates.Load<NodeContainerAR>(command.NodeContainerId);

            var addTerminalEquipmentResult = nodeContainerAR.AddTerminalEquipmentReference(commandContext, command.TerminalEquipmentId);

            if (addTerminalEquipmentResult.IsFailed)
                return Task.FromResult(addTerminalEquipmentResult);

            _eventStore.Aggregates.Store(terminalEquipmentAR);
            _eventStore.Aggregates.Store(nodeContainerAR);

            NotifyExternalServicesAboutChange(nodeContainer.RouteNodeId, command.TerminalEquipmentId);

            return Task.FromResult(placeTerminalEquipmentResult);
        }

        private async void NotifyExternalServicesAboutChange(Guid routeNodeId, Guid terminalEquipmentId)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("TerminalEquipment", ChangeTypeEnum.Addition, new Guid[] { terminalEquipmentId })
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

  