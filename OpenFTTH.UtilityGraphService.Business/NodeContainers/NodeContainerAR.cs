﻿using FluentResults;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Projections;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.NodeContainers
{
    /// <summary>
    /// The root structure placed in a route network node - i.e. cabinet, building, well, conduit closure etc.
    /// </summary>
    public class NodeContainerAR : AggregateBase
    {
        private NodeContainer? _container;

        public NodeContainerAR()
        {
            Register<NodeContainerPlacedInRouteNetwork>(Apply);
            Register<NodeContainerRemovedFromRouteNetwork>(Apply);
            Register<NodeContainerVerticalAlignmentReversed>(Apply);
            Register<NodeContainerManufacturerChanged>(Apply);
            Register<NodeContainerSpecificationChanged>(Apply);
            Register<NodeContainerRackAdded>(Apply);
            Register<NodeContainerTerminalEquipmentAdded>(Apply);
            Register<NodeContainerTerminalEquipmentsAddedToRack>(Apply);
            Register<NodeContainerTerminalEquipmentMovedToRack>(Apply);
        }

        #region Place in network

        public Result PlaceNodeContainerInRouteNetworkNode(
            CommandContext cmdContext,
            IReadOnlyDictionary<Guid, NodeContainer> nodeContainers,
            LookupCollection<NodeContainerSpecification> nodeContainerSpecifications,
            Guid nodeContainerId, 
            Guid nodeContainerSpecificationId,
            RouteNetworkInterest nodeOfInterest,
            NamingInfo? namingInfo,
            LifecycleInfo? lifecycleInfo,
            Guid? manufacturerId
        )
        {
            this.Id = nodeContainerId;

            if (nodeContainerId == Guid.Empty)
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY, "Node container id cannot be empty. A unique id must be provided by client."));

            if (nodeContainers.ContainsKey(nodeContainerId))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_NODE_CONTAINER_ID_ALREADY_EXISTS, $"A node container with id: {nodeContainerId} already exists."));

            if (nodeOfInterest.Kind != RouteNetworkInterestKindEnum.NodeOfInterest)
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_INTEREST_KIND_MUST_BE_NODE_OF_INTEREST, "Interest kind must be NodeOfInterest. You can only put node container into route nodes!"));

            if (!nodeContainerSpecifications.ContainsKey(nodeContainerSpecificationId))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_NODE_CONTAINER_SPECIFICATION_ID_NOT_FOUND, $"Cannot find node container specification with id: {nodeContainerSpecificationId}"));

            if (nodeContainers.Any(n => n.Value.RouteNodeId == nodeOfInterest.RouteNetworkElementRefs[0]))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.NODE_CONTAINER_ALREADY_EXISTS_IN_ROUTE_NODE, $"A node container already exist in the route node with id: {nodeOfInterest.RouteNetworkElementRefs[0]} Only one node container is allowed per route node.")); 

            var nodeContainer = new NodeContainer(nodeContainerId, nodeContainerSpecificationId, nodeOfInterest.Id, nodeOfInterest.RouteNetworkElementRefs[0])
            {
                ManufacturerId = manufacturerId,
                NamingInfo = namingInfo, 
                LifecycleInfo = lifecycleInfo
            };

            var nodeContainerPlaceInRouteNetworkEvent = new NodeContainerPlacedInRouteNetwork(nodeContainer)
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(nodeContainerPlaceInRouteNetworkEvent);

            return Result.Ok();
        }

        private void Apply(NodeContainerPlacedInRouteNetwork obj)
        {
            Id = obj.Container.Id;
            _container = obj.Container;
        }

        #endregion

        #region Remove from network
        public Result Remove(CommandContext cmdContext, List<SpanEquipment> relatedSpanEquipments)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (IsAnySpanEquipmentIsAffixedToContainer(relatedSpanEquipments))
            {
                return Result.Fail(new RemoveNodeContainerFromRouteNetworkError(
                    RemoveNodeContainerFromRouteNetworkErrorCodes.CANNOT_REMOVE_NODE_CONTAINER_WITH_AFFIXED_SPAN_EQUIPMENT,
                    $"Cannot remove a node container when span equipment(s) are affixed to it")
                );
            }

            var @event = new NodeContainerRemovedFromRouteNetwork(
               nodeContainerId: this.Id
            )
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(@event);

            return Result.Ok();
        }

        private bool IsAnySpanEquipmentIsAffixedToContainer(List<SpanEquipment> relatedSpanEquipments)
        {
            foreach (var spanEquipment in relatedSpanEquipments)
            {
                if (spanEquipment.NodeContainerAffixes != null)
                {
                    foreach (var affix in spanEquipment.NodeContainerAffixes)
                    {
                        if (affix.NodeContainerId == this.Id)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool IsAnyTerminalEquipmentsContainedInContainer(List<SpanEquipment> relatedSpanEquipments)
        {
            foreach (var spanEquipment in relatedSpanEquipments)
            {
                if (spanEquipment.NodeContainerAffixes != null)
                {
                    foreach (var affix in spanEquipment.NodeContainerAffixes)
                    {
                        if (affix.NodeContainerId == this.Id)
                            return true;
                    }
                }
            }

            return false;
        }

        private void Apply(NodeContainerRemovedFromRouteNetwork @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");
        }


        #endregion

        #region Reverse vertical content alignment
        public Result ReverseVerticalContentAlignment(CommandContext cmdContext)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            var reverseEvent = new NodeContainerVerticalAlignmentReversed(this.Id)
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(reverseEvent);

            return Result.Ok();
        }

        private void Apply(NodeContainerVerticalAlignmentReversed @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            _container = NodeContainerProjectionFunctions.Apply(_container, @event);
        }

        #endregion

        #region Place Rack

        public Result PlaceRack(CommandContext cmdContext, Guid rackId, Guid rackSpecificationId, string rackName, int? rackPosition, int rackHeightInUnits, LookupCollection<RackSpecification> rackSpecifications)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (!rackSpecifications.ContainsKey(rackSpecificationId))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_RACK_SPECIFICATION_ID_NOT_FOUND, $"Cannot find rack specification with id: {rackSpecificationId}"));

            if (rackPosition == null)
            {
                rackPosition = GetRackPosition();
            }

            if (ValidateRackNameAndPosition(rackName, rackPosition.Value).Errors.FirstOrDefault() is Error error)
                return Result.Fail(error);


            var @event = new NodeContainerRackAdded(
                nodeContainerId: this.Id,
                rackId: rackId,
                rackSpecificationId: rackSpecificationId,
                rackName: rackName,
                rackPosition: rackPosition.Value,
                rackHeightInUnits: rackHeightInUnits
            )
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(@event);

            return Result.Ok();
        }

        private int GetRackPosition()
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.Racks == null)
                return 1;

            int maxRackPos = 0;

            foreach (var rack in _container.Racks)
            {
                if (rack.Position > maxRackPos)
                    maxRackPos = rack.Position;
            }

            return maxRackPos + 1;
        }

        private void Apply(NodeContainerRackAdded @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            _container = NodeContainerProjectionFunctions.Apply(_container, @event);
        }

        private Result ValidateRackNameAndPosition(string rackName, int position)
        {
            if (String.IsNullOrEmpty(rackName))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_RACK_NAME_NOT_SPECIFIED, "Rack name is mandatory"));

            if (_container != null && _container.Racks != null && _container.Racks.Any(r => r.Name.ToLower() == rackName.ToLower()))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_RACK_NAME_NOT_UNIQUE, $"Rack name: '{rackName}' already used in node container with id: {this.Id}"));

            if (_container != null && _container.Racks != null && _container.Racks.Any(r => r.Position == position))
                return Result.Fail(new NodeContainerError(NodeContainerErrorCodes.INVALID_RACK_POSITION_NOT_UNIQUE, $"Rack position: {position} already used in node container with id: {this.Id}"));

            return Result.Ok();
        }

        #endregion

        #region Add Terminal Equipment To Node
        public Result AddTerminalEquipmentToNode(CommandContext cmdContext, Guid terminalEquipmentId)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            var e = new NodeContainerTerminalEquipmentAdded(this.Id, terminalEquipmentId)
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(e);

            return Result.Ok();
        }

        private void Apply(NodeContainerTerminalEquipmentAdded @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            _container = NodeContainerProjectionFunctions.Apply(_container, @event);
        }

        #endregion

        #region Add Terminal Equipments to Rack
        public Result AddTerminalEquipmentsToRack(CommandContext cmdContext, Guid[] terminalEquipmentIds, TerminalEquipmentSpecification terminalEquipmentSpecification, Guid rackId, int startUnitPosition, SubrackPlacmentMethod placmentMethod)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.Racks == null || !_container.Racks.Any(r => r.Id == rackId))
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.RACK_NOT_FOUND, $"Cannot find rack with id: {rackId} in node container with id: {this.Id}"));

            if (startUnitPosition < 0)
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.INVALID_RACK_UNIT_START_POSITION, $"Invalid rack unit must be greater or equal to zero"));

            var rack = _container.Racks.First(r => r.Id == rackId);

            var revisedStartPoistion = ReviseStartPosition(rack, startUnitPosition);

            var e = new NodeContainerTerminalEquipmentsAddedToRack(
                nodeContainerId: this.Id,
                rackId: rackId,
                startUnitPosition: revisedStartPoistion,
                terminalEquipmentIds: terminalEquipmentIds,
                terminalEquipmentHeightInUnits: terminalEquipmentSpecification.HeightInRackUnits
            )
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(e);

            return Result.Ok();
        }

        private void Apply(NodeContainerTerminalEquipmentsAddedToRack @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            _container = NodeContainerProjectionFunctions.Apply(_container, @event);
        }

        private int ReviseStartPosition(Rack rack, int startUnitPosition)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            foreach (var subrackMount in rack.SubrackMounts)
            {
                // If start position is within an exisiting equipment move to position where that equipment sits
                if (startUnitPosition > subrackMount.Position && startUnitPosition < (subrackMount.Position + subrackMount.HeightInUnits))
                    return subrackMount.Position;
            }

            return startUnitPosition;
        }

        #endregion


        #region Change Manufacturer
        public Result ChangeManufacturer(CommandContext cmdContext, Guid manufacturerId)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.ManufacturerId == manufacturerId)
            {
                return Result.Fail(new UpdateNodeContainerPropertiesError(
                       UpdateNodeContainerPropertiesErrorCodes.NO_CHANGE_TO_MANUFACTURER,
                       $"Will not change manufacturer, because the provided value is equal the existing value.")
                   );
            }

            var @event = new NodeContainerManufacturerChanged(
              nodeContainerId: this.Id,
              manufacturerId: manufacturerId
            )
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(NodeContainerManufacturerChanged @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            _container = NodeContainerProjectionFunctions.Apply(_container, @event);
        }

        #endregion

        #region Change Specification
        public Result ChangeSpecification(CommandContext cmdContext, NodeContainerSpecification currentSpecification, NodeContainerSpecification newSpecification)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.SpecificationId == newSpecification.Id)
            {
                return Result.Fail(new UpdateNodeContainerPropertiesError(
                       UpdateNodeContainerPropertiesErrorCodes.NO_CHANGE_TO_SPECIFICATION,
                       $"Will not change specification, because the provided specification id is the same as the existing one.")
                   );
            }


            var @event = new NodeContainerSpecificationChanged(
              nodeContainerId: this.Id,
              newSpecificationId: newSpecification.Id)
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(NodeContainerSpecificationChanged @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            _container = NodeContainerProjectionFunctions.Apply(_container, @event);
        }

        #endregion

        #region Change subrack Mount

        public Result ChangeSubrackMount(CommandContext cmdContext, Guid terminalEquipmentId, int terminalEquipmentHeightInUnits, Guid rackId, int startUnitPosition)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.Racks == null || !_container.Racks.Any(r => r.Id == rackId))
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.RACK_NOT_FOUND, $"Cannot find rack with id: {rackId} in node container with id: {this.Id}"));

            if (!TerminalEquipmentExistInRack(terminalEquipmentId))
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.TERMINAL_EQUIPMENT_NOT_FOUND_IN_ANY_RACK, $"Cannot find terminal equipment with id {terminalEquipmentId} in any rack within node container with id: {this.Id}"));

            if (CheckIfMovedToNewRack(terminalEquipmentId, rackId))
            {
                var newRack = _container.Racks.First(r => r.Id == rackId);

                var oldRack = GetCurrentRack(terminalEquipmentId);

                var revisedStartPoistion = ReviseStartPosition(newRack, startUnitPosition);


                var e = new NodeContainerTerminalEquipmentMovedToRack(
                    nodeContainerId: this.Id,
                    oldRackId: oldRack.Id,
                    newRackId: newRack.Id,
                    startUnitPosition: revisedStartPoistion,
                    terminalEquipmentId: terminalEquipmentId,
                    terminalEquipmentHeightInUnits: terminalEquipmentHeightInUnits
                )
                {
                    CorrelationId = cmdContext.CorrelationId,
                    IncitingCmdId = cmdContext.CmdId,
                    UserName = cmdContext.UserContext?.UserName,
                    WorkTaskId = cmdContext.UserContext?.WorkTaskId
                };

                RaiseEvent(e);

                return Result.Ok();
            }

            return Result.Ok();
        }

        private void Apply(NodeContainerTerminalEquipmentMovedToRack @event)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");
        }

        private bool CheckIfMovedToNewRack(Guid equipmentId, Guid possibleNewRackId)
        {
            var currentRackId = GetCurrentRack(equipmentId).Id;

            if (currentRackId != possibleNewRackId)
                return true;
            else
                return false;
        }

        private bool TerminalEquipmentExistInRack(Guid terminalEquipmentId)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.Racks == null)
                return false;

            foreach (var rack in _container.Racks)
            {
                foreach (var subrackMount in rack.SubrackMounts)
                {
                    if (subrackMount.TerminalEquipmentId == terminalEquipmentId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Rack GetCurrentRack(Guid terminalEquipmentId)
        {
            if (_container == null)
                throw new ApplicationException($"Invalid internal state. Node container property cannot be null. Seems that node container has never been created. Please check command handler logic.");

            if (_container.Racks == null)
                throw new ApplicationException($"No racks in node equipment with id: {_container.Id}");

            foreach (var rack in _container.Racks)
            {
                foreach (var subrackMount in rack.SubrackMounts)
                {
                    if (subrackMount.TerminalEquipmentId == terminalEquipmentId)
                    {
                        return rack;
                    }
                }
            }

            throw new ApplicationException($"Can't find terminal equipment with id: {terminalEquipmentId} in any racks.");
        }

        #endregion
    }
}
