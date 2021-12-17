using FluentResults;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Events;
using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments
{
    /// <summary>
    /// A equipment place in a node/rack - i.e. splice closures, OLTs etc.
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
               terminalStructures: CreateTerminalStructuresFromSpecification(terminalEquipmentSpecifications[terminalEquipmentSpecificationId], terminalStructureSpecifications),
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

        private TerminalStructure[] CreateTerminalStructuresFromSpecification(TerminalEquipmentSpecification terminalEquipmentSpecification, LookupCollection<TerminalStructureSpecification> terminalStructureSpecifications)
        {
            List<TerminalStructure> terminalStructures = new();

            foreach (var structureTemplate in terminalEquipmentSpecification.StructureTemplates)
            {
                if (terminalStructureSpecifications.TryGetValue(structureTemplate.TerminalStructureSpecificationId, out var terminalStructureSpecification))
                {
                    List<Terminal> terminals = new();

                    foreach (var terminalTemplate in terminalStructureSpecification.TerminalTemplates)
                    {
                        terminals.Add(
                            new Terminal(Guid.NewGuid(), terminalTemplate.Name, terminalTemplate.Direction, terminalTemplate.IsPigtail, terminalTemplate.IsSplice, terminalTemplate.ConnectorType)
                        );
                    }

                    terminalStructures.Add(new TerminalStructure(Guid.NewGuid(), structureTemplate.TerminalStructureSpecificationId, structureTemplate.Position, terminals.ToArray()));
                }
                else
                {
                    throw new ApplicationException($"Invalid/corrupted terminal equipment specification: {terminalEquipmentSpecification.Id} References a non-existing terminal structure specification with id: {structureTemplate.TerminalStructureSpecificationId}");
                }
            }

            return terminalStructures.ToArray();
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
