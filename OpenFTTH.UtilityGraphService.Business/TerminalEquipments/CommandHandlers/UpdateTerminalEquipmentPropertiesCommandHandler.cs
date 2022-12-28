using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments.CommandHandlers
{
    public class UpdateTerminalEquipmentPropertiesCommandHandler : ICommandHandler<UpdateTerminalEquipmentProperties, Result>
    {
        private readonly IEventStore _eventStore;
        private readonly IExternalEventProducer _externalEventProducer;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;

        public UpdateTerminalEquipmentPropertiesCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
        }

        public Task<Result> HandleAsync(UpdateTerminalEquipmentProperties command)
        {
            var terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            // Because the client is allowed to provide either a span equipment or segment id, we need look it up via the utility network graph
            if (!utilityNetwork.TryGetEquipment<TerminalEquipment>(command.TerminalEquipmentId, out TerminalEquipment terminalEquipment))
                return Task.FromResult(Result.Fail(new UpdateEquipmentPropertiesError(UpdateEquipmentPropertiesErrorCodes.TERMINAL_EQUIPMENT_NOT_FOUND, $"Cannot find any terminal equipment in the utility graph with id: {command.TerminalEquipmentId}")));

            if (!utilityNetwork.TryGetEquipment<NodeContainer>(terminalEquipment.NodeContainerId, out NodeContainer nodeContainer))
                return Task.FromResult(Result.Fail(new UpdateEquipmentPropertiesError(UpdateEquipmentPropertiesErrorCodes.NODE_CONTAINER_NOT_FOUND, $"Cannot find any node container with id: {terminalEquipment.NodeContainerId}")));

            var terminalEquipmentAR = _eventStore.Aggregates.Load<TerminalEquipmentAR>(terminalEquipment.Id);

            bool somethingChanged = false;

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            // Check if naming info has been updated
            if (command.NamingInfo != null && !command.NamingInfo.Equals(terminalEquipment.NamingInfo))
            {
                var updateNamingInfoResult = terminalEquipmentAR.ChangeNamingInfo(
                    cmdContext: commandContext,
                    command.NamingInfo
                );

                if (updateNamingInfoResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateNamingInfoResult.Errors.First()));

                somethingChanged = true;
            }


            // Check if address info has been updated
            if (command.AddressInfo != null && !command.AddressInfo.Equals(terminalEquipment.AddressInfo))
            {
                var updateAddressInfoResult = terminalEquipmentAR.ChangeAddressInfo(
                    cmdContext: commandContext,
                    command.AddressInfo
                );

                if (updateAddressInfoResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateAddressInfoResult.Errors.First()));

                somethingChanged = true;
            }


            // Check if manufacturer as been updated
            if (command.ManufacturerId != null && !command.ManufacturerId.Equals(terminalEquipment.ManufacturerId))
            {
                var updateManufacturerInfoResult = terminalEquipmentAR.ChangeManufacturer(
                    cmdContext: commandContext,
                    command.ManufacturerId.Value
                );

                if (updateManufacturerInfoResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateManufacturerInfoResult.Errors.First()));

                somethingChanged = true;
            }

            // Check if specification has been updated
            if (command.SpecificationId != null && !command.SpecificationId.Equals(terminalEquipment.SpecificationId))
            {
                if (!terminalEquipmentSpecifications.ContainsKey(command.SpecificationId.Value))
                {
                    return Task.FromResult(Result.Fail(new UpdateEquipmentPropertiesError(UpdateEquipmentPropertiesErrorCodes.SPAN_SPECIFICATION_NOT_FOUND, $"Cannot find any span equipment specification with id: {command.SpecificationId.Value}")));
                }

                var updateSpecificationResult = terminalEquipmentAR.ChangeSpecification(
                    cmdContext: commandContext,
                    terminalEquipmentSpecifications[terminalEquipment.SpecificationId],
                    terminalEquipmentSpecifications[command.SpecificationId.Value]
                );

                if (updateSpecificationResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateSpecificationResult.Errors.First()));

                somethingChanged = true;
            }

            // Check if rack information is present and has changed
            if (command.RackId != null && command.StartUnitPosition != null && CheckIfRackInfoHasChanged(command, nodeContainer, terminalEquipment))
            {
                var nodeContainerAR = _eventStore.Aggregates.Load<NodeContainerAR>(nodeContainer.Id);

                var terminalEquipmentSpecification = terminalEquipmentSpecifications[terminalEquipment.SpecificationId];

                var updateSubrackMountResult = nodeContainerAR.ChangeSubrackMount(commandContext, terminalEquipment.Id, terminalEquipmentSpecification.HeightInRackUnits, command.RackId.Value, command.StartUnitPosition.Value);

                if (updateSubrackMountResult.IsFailed)
                    return Task.FromResult(Result.Fail(updateSubrackMountResult.Errors.First()));

                somethingChanged = true;
            }


            if (somethingChanged)
            {
                _eventStore.Aggregates.Store(terminalEquipmentAR);

                NotifyExternalServicesAboutSpanEquipmentChange(terminalEquipment.Id, nodeContainer.RouteNodeId);

                return Task.FromResult(Result.Ok());
            }
            else
            {
                return Task.FromResult(Result.Fail(new UpdateEquipmentPropertiesError(
                      UpdateEquipmentPropertiesErrorCodes.NO_CHANGE,
                      $"Will not update terminal equipment, because no difference found in provided arguments compared to the current values of the terminal equipment.")
                  ));
            }
        }

        private bool CheckIfRackInfoHasChanged(UpdateTerminalEquipmentProperties command, NodeContainer nodeContainer, TerminalEquipment terminalEquipment)
        {
            if (command.RackId == null || command.RackId.Value == Guid.Empty)
                return false;

            if (nodeContainer.Racks == null)
                return false;

            // Find current rack id and position
            Guid currentRackId = Guid.Empty;
            int currentPosition = -1;

            foreach (var rack in nodeContainer.Racks)
            {
                foreach (var subrackMount in rack.SubrackMounts)
                {
                    if (subrackMount.TerminalEquipmentId == terminalEquipment.Id)
                    {
                        currentRackId = rack.Id;
                        currentPosition = subrackMount.Position;
                    }
                }
            }

            if (currentRackId == Guid.Empty)
                throw new ApplicationException($"Cannot find any rack with id: {command.RackId} in node container with id: {nodeContainer.Id}");

            if (command.RackId != currentRackId)
                return true;

            if (command.StartUnitPosition != currentPosition)
                return true;

            return false;
        }

        private async void NotifyExternalServicesAboutSpanEquipmentChange(Guid spanEquipmentId, Guid routeNodeId)
        {
            var idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("TerminalEquipment", ChangeTypeEnum.Modification, new Guid[] { spanEquipmentId })
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
                    affectedRouteNetworkElementIds: new Guid[] { routeNodeId }
                );

            await _externalEventProducer.Produce(
                nameof(RouteNetworkElementContainedEquipmentUpdated),
                updatedEvent);
        }
    }
}
