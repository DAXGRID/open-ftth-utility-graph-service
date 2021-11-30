using FluentResults;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments
{
    /// <summary>
    /// The root structure placed in a route network node - i.e. cabinet, building, well, conduit closure etc.
    /// </summary>
    public class TerminalEquipmentAR : AggregateBase
    {
        private TerminalEquipment? _terminalEquipment;

        public TerminalEquipmentAR()
        {
            Register<TerminalEquipmentPlacedInNodeContainer>(Apply);
        }

        #region Place

        public Result Place(
            CommandContext cmdContext,
            LookupCollection<TerminalEquipmentSpecification> terminalEquipmentSpecifications,
            LookupCollection<TerminalStructureSpecification> terminalStructureSpecifications,
            Guid nodeContainerId,
            Guid terminalEquipmentId,
            Guid terminalEquipmentSpecificationId,
            NamingInfo? namingInfo,
            LifecycleInfo? lifecycleInfo,
            Guid? manufacturerId
        )
        {
            this.Id = terminalEquipmentId;

            if (terminalEquipmentId == Guid.Empty)
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.INVALID_TERMINAL_EQUIPMENT_ID_CANNOT_BE_EMPTY, "Terminal equipment id cannot be empty. A unique id must be provided by client."));

            if (nodeContainerId == Guid.Empty)
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.INVALID_NODE_CONTAINER_ID_CANNOT_BE_EMPTY, "Node container id cannot be empty. Must reference an exiting node container."));

            if (!terminalEquipmentSpecifications.ContainsKey(terminalEquipmentSpecificationId))
                return Result.Fail(new TerminalEquipmentError(TerminalEquipmentErrorCodes.INVALID_TERMINAL_EQUIPMENT_SPECIFICATION_ID_NOT_FOUND, $"Cannot find terminal specification with id: {terminalEquipmentSpecificationId}"));


            var terminalEquipment = new TerminalEquipment
            (
               id: terminalEquipmentId,
               specificationId: terminalEquipmentSpecificationId,
               nodeContainerId: nodeContainerId,
               terminalStructures: new TerminalStructure[] { },
               manufacturerId: manufacturerId,
               namingInfo: namingInfo,
               lifecycleInfo: lifecycleInfo
            );

            var terminalEquipmentPlacedInNodeContainerEvent = new TerminalEquipmentPlacedInNodeContainer(terminalEquipment)
            {
                CorrelationId = cmdContext.CorrelationId,
                IncitingCmdId = cmdContext.CmdId,
                UserName = cmdContext.UserContext?.UserName,
                WorkTaskId = cmdContext.UserContext?.WorkTaskId
            };

            RaiseEvent(terminalEquipmentPlacedInNodeContainerEvent);

            return Result.Ok();
        }

        private void Apply(TerminalEquipmentPlacedInNodeContainer obj)
        {
            Id = obj.Equipment.Id;
            _terminalEquipment = obj.Equipment;
        }

        #endregion
    }
}
