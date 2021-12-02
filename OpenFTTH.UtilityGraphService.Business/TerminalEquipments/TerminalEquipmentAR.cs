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
            int sequenceNumber,
            TerminalEquipmentNamingMethodEnum namingMethod,
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
               namingInfo: CalculateName(namingInfo, sequenceNumber, namingMethod),
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

        private NamingInfo CalculateName(NamingInfo? namingInfo, int sequenceNumber, TerminalEquipmentNamingMethodEnum namingMethod)
        {
            NamingInfo resultNamingInfo = new();

            resultNamingInfo.Description = namingInfo?.Description;

            switch (namingMethod)
            {
                case TerminalEquipmentNamingMethodEnum.NumberOnly:
                    resultNamingInfo.Name = sequenceNumber.ToString();
                    break;

                case TerminalEquipmentNamingMethodEnum.NameOnly:
                    resultNamingInfo.Name = namingInfo?.Name;
                    break;

                case TerminalEquipmentNamingMethodEnum.NameAndNumber:
                    if (namingInfo != null && !String.IsNullOrEmpty(namingInfo.Name))
                        resultNamingInfo.Name = namingInfo.Name + " " + sequenceNumber.ToString();
                    else
                        resultNamingInfo.Name = sequenceNumber.ToString();
                    break;
            }

            return resultNamingInfo;
        }

        private void Apply(TerminalEquipmentPlacedInNodeContainer obj)
        {
            Id = obj.Equipment.Id;
            _terminalEquipment = obj.Equipment;
        }

        #endregion
    }
}
