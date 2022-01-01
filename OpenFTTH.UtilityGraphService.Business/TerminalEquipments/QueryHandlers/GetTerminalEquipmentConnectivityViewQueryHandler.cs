﻿using DAX.ObjectVersioning.Graph;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
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

            var equipmentData = GatherRelevantTerminalEquipmentData(terminalEquipment);

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
                                A = GetAEndInfo(equipmentData, terminal),
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

        private TerminalEquipmentConnectivityViewEndInfo GetAEndInfo(RelevantEquipmentData relevantEquipmentData, Terminal terminal)
        {
            var terminalInfo = new TerminalEquipmentConnectivityViewTerminalInfo(terminal.Id, terminal.Name);

            var traceInfo = relevantEquipmentData.TracedTerminals[terminal.Id].Z;

            var connectedToText = CreateConnectedToString(relevantEquipmentData, traceInfo);

            return new TerminalEquipmentConnectivityViewEndInfo(terminalInfo)
            {
                ConnectedTo = connectedToText
            };
        }

        private string? CreateConnectedToString(RelevantEquipmentData relevantEquipmentData, TraceEndInfo? traceInfo)
        {
            if (traceInfo == null)
                return null;

            var spanEquipment = traceInfo.NeighborSegment.SpanEquipment(_utilityNetwork);
            var fiber = traceInfo.NeighborSegment.StructureIndex;

            return  $"{spanEquipment.Name} ({spanEquipment.SpanStructures.Length - 1}) Fiber {fiber}";
        }

        private RelevantEquipmentData GatherRelevantTerminalEquipmentData(TerminalEquipment terminalEquipment)
        {
            RelevantEquipmentData relevantEquipmentData = new RelevantEquipmentData();

            relevantEquipmentData.TracedTerminals = TraceAllTerminals(terminalEquipment);

            var endNodesIds = GetEndNodeIdsFromTraceResult(relevantEquipmentData.TracedTerminals.Values);

            relevantEquipmentData.RouteNetworkElements = GatherRelevantRouteNodeInformation(_queryDispatcher, endNodesIds);

            return relevantEquipmentData;
        }

        private Dictionary<Guid, TraceInfo> TraceAllTerminals(TerminalEquipment terminalEquipment)
        {
            Dictionary<Guid, TraceInfo> traceInfosByTerminalId = new();

            // Trace all equipment terminals
            foreach (var terminalStructure in terminalEquipment.TerminalStructures)
            {
                foreach (var terminal in terminalStructure.Terminals)
                {
                    TraceInfo traceInfo = new TraceInfo();

                    var terminalTraceResult = _utilityNetwork.Graph.Trace(terminal.Id);

                    if (terminalTraceResult != null)
                    {
                        if (terminalTraceResult.Upstream.Length > 0)
                        {
                            traceInfo.Upstream = GetEndInfoFromTrace(terminal.Id, terminalTraceResult.Upstream);
                        }

                        if (terminalTraceResult.Downstream.Length > 0)
                        {
                            traceInfo.Downstream = GetEndInfoFromTrace(terminal.Id, terminalTraceResult.Downstream);
                        }
                    }

                    traceInfosByTerminalId.Add(terminal.Id, traceInfo);
                }
            }

            return traceInfosByTerminalId;
        }

        private IEnumerable<Guid> GetEndNodeIdsFromTraceResult(IEnumerable<TraceInfo> traceInfos)
        {
            HashSet<Guid> endNodeIds = new();

            foreach (var traceInfo in traceInfos)
            {
                AddEndNodeIdsToHash(traceInfo, endNodeIds);
            }

            return endNodeIds;
        }

        private static void AddEndNodeIdsToHash(TraceInfo traceInfo, HashSet<Guid> endNodeIds)
        {
            if (traceInfo.Upstream != null)
            {
                if (!endNodeIds.Contains(traceInfo.Upstream.EndTerminal.RouteNodeId))
                    endNodeIds.Add(traceInfo.Upstream.EndTerminal.RouteNodeId);
            }

            if (traceInfo.Downstream != null)
            {
                if (!endNodeIds.Contains(traceInfo.Downstream.EndTerminal.RouteNodeId))
                    endNodeIds.Add(traceInfo.Downstream.EndTerminal.RouteNodeId);
            }
        }

        private TraceEndInfo GetEndInfoFromTrace(Guid tracedTerminalId, IGraphObject[] trace)
        {
            if (trace.Length < 2)
                throw new ApplicationException($"Expected trace length to be minimum 2. Please check trace on terminal with id: {tracedTerminalId}");


            // Get neighbor segment
            var neighborSegment = trace.First();

            if (!(neighborSegment is UtilityGraphConnectedSegment))
                throw new ApplicationException($"Expected neighbor to be a UtilityGraphConnectedSegment. Please check trace on terminal with id: {tracedTerminalId}");


            // Get end terminal
            var terminalEnd = trace.Last();

            if (!(terminalEnd is UtilityGraphConnectedTerminal))
                throw new ApplicationException($"Expected end to be a UtilityGraphConnectedTerminal. Please check trace on terminal with id: {tracedTerminalId}");


            return new TraceEndInfo((UtilityGraphConnectedSegment)neighborSegment, (UtilityGraphConnectedTerminal)terminalEnd);
        }

        private LookupCollection<RouteNetworkElement> GatherRelevantRouteNodeInformation(IQueryDispatcher queryDispatcher, IEnumerable<Guid> nodeOfInterestIds)
        {
            if (nodeOfInterestIds.Count() == 0)
                return new LookupCollection<RouteNetworkElement>();

            RouteNetworkElementIdList idList = new();
            idList.AddRange(nodeOfInterestIds);

            var interestQueryResult = queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
                new GetRouteNetworkDetails(idList)
                {
                    RouteNetworkElementFilter = new RouteNetworkElementFilterOptions() { 
                        IncludeNamingInfo = true,
                        IncludeRouteNodeInfo = true
                    }
                }
            ).Result;

            if (interestQueryResult.IsFailed)
                throw new ApplicationException("Failed to query route network information. Got error: " + interestQueryResult.Errors.First().Message);

            return interestQueryResult.Value.RouteNetworkElements;
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

        private record RelevantEquipmentData
        {
            public Dictionary<Guid, TraceInfo> TracedTerminals { get; set; }
            public LookupCollection<RouteNetworkElement> RouteNetworkElements { get; set; }
        }

        private record TraceInfo
        {
            public TraceEndInfo? Upstream { get; set; }
            public TraceEndInfo? Downstream { get; set; }

            public bool DownstreamIsZ {get; set;}
            public TraceEndInfo? Z
            {
                get
                {
                    if (DownstreamIsZ) return Downstream;
                    else return Upstream;
                }
            }
            
        }

        private record TraceEndInfo
        {
            public UtilityGraphConnectedSegment NeighborSegment { get; set; }
            public UtilityGraphConnectedTerminal EndTerminal { get; set; }

            public TraceEndInfo(UtilityGraphConnectedSegment neighborSegment, UtilityGraphConnectedTerminal endTerminal)
            {
                NeighborSegment = neighborSegment;
                EndTerminal = endTerminal;
            }
        }
    }
}
