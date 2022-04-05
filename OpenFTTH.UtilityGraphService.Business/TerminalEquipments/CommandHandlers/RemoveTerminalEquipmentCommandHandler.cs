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
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers;
using OpenFTTH.UtilityGraphService.Business.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments.CommandHandlers
{
    public class RemoveTerminalEquipmentCommandHandler : ICommandHandler<RemoveTerminalEquipment, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;

        public RemoveTerminalEquipmentCommandHandler(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
        }

        public Task<Result> HandleAsync(RemoveTerminalEquipment command)
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            if (!utilityNetwork.TryGetEquipment<TerminalEquipment>(command.TerminalEquipmentId, out var terminalEquipment))
            {
                return Task.FromResult(Result.Fail(new RemoveTerminalEquipmentError(RemoveTerminalEquipmentErrorCodes.TERMINAL_EQUIPMENT_NOT_FOUND, $"Cannot find any terminal equipment with id: {command.TerminalEquipmentId}")));
            }

            if (!utilityNetwork.TryGetEquipment<NodeContainer>(terminalEquipment.NodeContainerId, out var nodeContainer))
            {
                throw new ApplicationException($"Error looking up node container by id: {terminalEquipment.NodeContainerId} referenced by terminal equipment with id: {terminalEquipment.Id}");
            }


            var terminalEquipmentAR = _eventStore.Aggregates.Load<TerminalEquipmentAR>(command.TerminalEquipmentId);

            var commandContext = new CommandContext(command.CorrelationId, command.CmdId, command.UserContext);

            /*
            var removeResult = terminalEquipmentAR.Remove(commandContext);

            if (removeResult.IsSuccess)
            {
                // Remember to remove the walk of interest as well
                var unregisterInterestCmd = new UnregisterInterest(commandContext.CorrelationId, commandContext.UserContext, nodeContainer.InterestId);

                var unregisterInterestCmdResult = _commandDispatcher.HandleAsync<UnregisterInterest, Result>(unregisterInterestCmd).Result;

                if (unregisterInterestCmdResult.IsFailed)
                    throw new ApplicationException($"Failed to unregister interest: {nodeContainer.InterestId} of node container: {nodeContainer.Id} in RemoveNodeContainerFromRouteNetworkCommandHandler Error: {unregisterInterestCmdResult.Errors.First().Message}");

                _eventStore.Aggregates.Store(terminalEquipmentAR);
                NotifyExternalServicesAboutChange(nodeContainer);
            }

            return Task.FromResult(removeResult);
            */

            return null;
        }

        private List<SpanEquipment> GetRelatedSpanEquipments(Guid routeNodeId)
        {
            // Get interest information for all equipments in node
            var queryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(new GetRouteNetworkDetails(new RouteNetworkElementIdList() { routeNodeId })).Result;

            if (queryResult.IsFailed)
                throw new ApplicationException($"Got unexpected error result: {queryResult.Errors.First().Message} trying to query interest information for node container and/or span equipment while processing the AffixSpanEquipmentToNodeContainer command");

            if (queryResult.Value.RouteNetworkElements == null)
                throw new ApplicationException($"Got unexpected result querying route node: {routeNodeId} Expected one route node but got null");

            if (queryResult.Value.RouteNetworkElements.Count != 1)
                throw new ApplicationException($"Got unexpected result querying route node: {routeNodeId} Expected one route node but got: {queryResult.Value.RouteNetworkElements.Count}");


            var _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            List<SpanEquipment> result = new();

            var routeNode = queryResult.Value.RouteNetworkElements.First();

            if (routeNode.InterestRelations != null)
            {
                foreach (var interestRel in routeNode.InterestRelations)
                {
                    // Find span equipment
                    if (_utilityNetwork.TryGetEquipment<SpanEquipment>(interestRel.RefId, out SpanEquipment spanEquipment))
                    {
                        result.Add(spanEquipment);
                    }
                }
            }

            return result;
        }

        private async void NotifyExternalServicesAboutChange(NodeContainer nodeContainer)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("NodeContainer", ChangeTypeEnum.Addition, new Guid[] { nodeContainer.Id })
            };

            var updatedEvent =
                new RouteNetworkElementContainedEquipmentUpdated(
                    eventType: typeof(RouteNetworkElementContainedEquipmentUpdated).Name,
                    eventId: Guid.NewGuid(),
                    eventTimestamp: DateTime.UtcNow,
                    applicationName: "UtilityNetworkService",
                    applicationInfo: null,
                    category: "EquipmentDeletion",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: new Guid[] { nodeContainer.RouteNodeId }
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }
    }
}

  