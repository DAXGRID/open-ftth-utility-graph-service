using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Projections;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments.QueryHandling
{
    public class GetTerminalEquipmentConnectivityViewQueryHandler
        : IQueryHandler<GetTerminalEquipmentConnectivityView, Result<TerminalEquipmentAZConnectivityViewModel>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<RackSpecification> _rackSpecifications;
        private LookupCollection<TerminalStructureSpecification> _terminalStructureSpecifications;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;

        public GetTerminalEquipmentConnectivityViewQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result<TerminalEquipmentAZConnectivityViewModel>> HandleAsync(GetTerminalEquipmentConnectivityView query)
        {
            _rackSpecifications = _eventStore.Projections.Get<RackSpecificationsProjection>().Specifications;
            _terminalStructureSpecifications = _eventStore.Projections.Get<TerminalStructureSpecificationsProjection>().Specifications;
            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;

            // If terminal equipment   
            if (_utilityNetwork.TryGetEquipment<TerminalEquipment>(query.terminalEquipmentOrRackId, out var terminalEquipment))
            {
                return Task.FromResult(
                    Result.Ok(BuildStandaloneTerminalEquipmentAZView(query, terminalEquipment))
                );
            }
            else
            {
                var getNodeContainerResult = QueryHelper.GetNodeContainerFromRouteNodeId(_queryDispatcher, query.routeNodeId);

                if (getNodeContainerResult.IsFailed)
                    return Task.FromResult(Result.Fail<TerminalEquipmentAZConnectivityViewModel>(getNodeContainerResult.Errors.First()));

                var nodeContainer = getNodeContainerResult.Value;

                if (nodeContainer == null)
                    throw new ApplicationException("There a bug in QueryHelper.GetNodeContainerFromRouteNodeId query. Cannot just return success and a null node container. Please check.");

                if (nodeContainer.Racks == null || !nodeContainer.Racks.Any(r => r.Id == query.terminalEquipmentOrRackId))
                    return Task.FromResult(Result.Fail<TerminalEquipmentAZConnectivityViewModel>(new TerminalEquipmentError(TerminalEquipmentErrorCodes.RACK_NOT_FOUND, $"Cannot find rack with id: {query.terminalEquipmentOrRackId} in node container with id: {nodeContainer.Id}")));

                return Task.FromResult(
                    Result.Ok(BuildRackWithTerminalEquipmentAZView(query, nodeContainer))
                );
            }
        }

        private TerminalEquipmentAZConnectivityViewModel BuildRackWithTerminalEquipmentAZView(GetTerminalEquipmentConnectivityView query, NodeContainer nodeContainer)
        {
            if (nodeContainer.Racks == null)
                throw new ApplicationException("There a bug in code. Caller must check if rack exists.");

            var rack = nodeContainer.Racks.First(r => r.Id == query.terminalEquipmentOrRackId);

            var rackSpec = _rackSpecifications[rack.SpecificationId];

            TerminalEquipmentConnectivityViewNodeStructureInfo rackStructure =
                new TerminalEquipmentConnectivityViewNodeStructureInfo(rack.Id, "Rack", rack.Name, rackSpec.Name);

            List<TerminalEquipmentConnectivityViewEquipmentInfo> equipmentInfos = new();

            foreach (var mount in rack.SubrackMounts)
            {
                if (_utilityNetwork.TryGetEquipment<TerminalEquipment>(mount.TerminalEquipmentId, out var terminalEquipment))
                {
                    equipmentInfos.Add(BuildTerminalEquipmentView(query, terminalEquipment, rack.Id));
                }
                else
                {
                    throw new ApplicationException($"Cannot find terminal equipment with id: {mount.TerminalEquipmentId} in route node: {query.routeNodeId}");
                }
            }

            return (
                new TerminalEquipmentAZConnectivityViewModel(                    
                    terminalEquipments: equipmentInfos.ToArray()
                )
                {
                    ParentNodeStructures = new TerminalEquipmentConnectivityViewNodeStructureInfo[] { rackStructure }
                }
            );
        }

        private TerminalEquipmentAZConnectivityViewModel BuildStandaloneTerminalEquipmentAZView(GetTerminalEquipmentConnectivityView query, TerminalEquipment terminalEquipment)
        {
            return (
                new TerminalEquipmentAZConnectivityViewModel(
                    terminalEquipments: new TerminalEquipmentConnectivityViewEquipmentInfo[] {
                       BuildTerminalEquipmentView(query, terminalEquipment)
                    }
                )
            );
        }

        private TerminalEquipmentConnectivityViewEquipmentInfo BuildTerminalEquipmentView(GetTerminalEquipmentConnectivityView query, TerminalEquipment terminalEquipment, Guid? parentStructureId = null)
        {
            if (!_terminalEquipmentSpecifications.TryGetValue(terminalEquipment.SpecificationId, out var terminalEquipmentSpecification))
                throw new ApplicationException($"Invalid/corrupted terminal equipment instance: {terminalEquipment.Id} Has reference to non-existing terminal equipment specification with id: {terminalEquipment.SpecificationId}");

            List<TerminalEquipmentConnectivityViewTerminalStructureInfo> terminalStructureInfos = new();

            foreach (var terminalStructure in terminalEquipment.TerminalStructures)
            {
                if (!_terminalStructureSpecifications.TryGetValue(terminalStructure.SpecificationId, out var terminalStructureSpecification))
                    throw new ApplicationException($"Invalid/corrupted terminal equipment specification: {terminalEquipment.SpecificationId} has reference to non-existing terminal structure specification with id: {terminalStructure.SpecificationId}");

                List<TerminalEquipmentAZConnectivityViewLineInfo> lineInfos = new();

                foreach (var terminal in terminalStructure.Terminals)
                {
                    if (terminal.Direction == TerminalDirectionEnum.BI)
                    {
                        lineInfos.Add(
                            new TerminalEquipmentAZConnectivityViewLineInfo(GetConnectorSymbol(terminal, terminal))
                            {
                                A = new TerminalEquipmentConnectivityViewEndInfo(
                                    new TerminalEquipmentConnectivityViewTerminalInfo(terminal.Id, terminal.Name)
                                ),
                                Z = new TerminalEquipmentConnectivityViewEndInfo(
                                    new TerminalEquipmentConnectivityViewTerminalInfo(terminal.Id, terminal.Name)
                                ),
                            }
                        );
                    }
                }

                terminalStructureInfos.Add(
                    new TerminalEquipmentConnectivityViewTerminalStructureInfo(
                        id: terminalStructure.Id,
                        category: terminalStructureSpecification.Category,
                        name: terminalStructure.Name,
                        specName: terminalStructureSpecification.Name,
                        lines: lineInfos.ToArray()
                    )
                );
            }

            return (
                new TerminalEquipmentConnectivityViewEquipmentInfo(
                       id: terminalEquipment.Id,
                       category: terminalEquipmentSpecification.Category,
                       name: terminalEquipment.Name == null ? "NO NAME" : terminalEquipment.Name,
                       specName: terminalEquipmentSpecification.Name,
                       terminalStructures: terminalStructureInfos.ToArray()
                   )
                { 
                    ParentNodeStructureId = parentStructureId
                }
            );
        }

        private string GetConnectorSymbol(Terminal fromTerminal, Terminal toTerminal)
        {
            string symbolName = "";

            if (fromTerminal.IsSplice)
                symbolName += "Splice";
            else
                symbolName += "Patch";

            if (fromTerminal != toTerminal)
            {
                if (toTerminal.IsSplice)
                    symbolName += "Splice";
                else
                    symbolName += "Patch";
            }

            return symbolName;
        }
    }
}
