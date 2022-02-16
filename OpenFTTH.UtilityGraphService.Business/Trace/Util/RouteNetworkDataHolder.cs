using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Trace.Util
{
    public class RouteNetworkDataHolder
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;
        private LookupCollection<TerminalStructureSpecification> _terminalStructureSpecifications;

        public LookupCollection<RouteNetworkElement> RouteNetworkElements { get; set; }

        public Dictionary<Guid, NodeContainer> NodeContainerByNodeId = new();

        public RouteNetworkDataHolder(IEventStore eventStore, UtilityNetworkProjection utilityNetwork, IQueryDispatcher queryDispatcher, IEnumerable<Guid> nodeOfInterestIds)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = utilityNetwork;

            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
            _terminalStructureSpecifications = _eventStore.Projections.Get<TerminalStructureSpecificationsProjection>().Specifications;

            if (nodeOfInterestIds.Count() == 0)
            {
                RouteNetworkElements = new LookupCollection<RouteNetworkElement>();
                return;
            }


            RouteNetworkElementIdList idList = new();
            idList.AddRange(nodeOfInterestIds);

            var routeNetworkQueryResult = queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
                new GetRouteNetworkDetails(idList)
                {
                    RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementOnly,
                    RouteNetworkElementFilter = new RouteNetworkElementFilterOptions()
                    {
                        IncludeNamingInfo = true,
                        IncludeRouteNodeInfo = true
                    }
                }
            ).Result;

            if (routeNetworkQueryResult.IsFailed)
                throw new ApplicationException("Failed to query route network information. Got error: " + routeNetworkQueryResult.Errors.First().Message);

            RouteNetworkElements = routeNetworkQueryResult.Value.RouteNetworkElements;

            // Get node containers
            foreach (var routeNetworkElement in RouteNetworkElements)
            {
                if (routeNetworkElement.InterestRelations != null)
                {
                    foreach (var interestRel in routeNetworkElement.InterestRelations)
                    {
                        if (utilityNetwork.TryGetEquipment<NodeContainer>(interestRel.RefId, out var nodeContainer))
                        {
                            NodeContainerByNodeId.Add(routeNetworkElement.Id, nodeContainer);
                        }
                    }
                }
            }
        }

        public string? GetNodeName(Guid routeNodeId)
        {
            if (RouteNetworkElements != null && RouteNetworkElements.ContainsKey(routeNodeId))
            {
                var node = RouteNetworkElements[routeNodeId];
                return node.Name;
            }

            return null;
        }

        public string? GetRackName(Guid routeNodeId, Guid terminalEquipmentId)
        {
            if (NodeContainerByNodeId.ContainsKey(routeNodeId))
            {
                var container = NodeContainerByNodeId[routeNodeId];

                if (container.Racks != null)
                {
                    foreach (var rack in container.Racks)
                    {
                        if (rack.SubrackMounts != null && rack.SubrackMounts.Any(m => m.TerminalEquipmentId == terminalEquipmentId))
                            return rack.Name;
                    }
                }
            }

            return null;
        }

        public string? GetNodeAndEquipmentEndString(UtilityGraphConnectedTerminal terminalRef)
        {
            var nodeName = GetNodeName(terminalRef.RouteNodeId);

            if (terminalRef.IsDummyEnd)
                return $"{nodeName} løs ende";

            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

            var terminalStructure = terminalRef.TerminalStructure(_utilityNetwork);

            var terminal = terminalRef.Terminal(_utilityNetwork);

            if (nodeName != null)
                nodeName += " ";

            var equipmentName = GetEquipmentWithStructureInfoString(terminalRef);

            return $"{nodeName}{equipmentName}";
        }

        public string GetEquipmentWithoutStructureInfoString(IUtilityGraphTerminalRef terminalRef, bool includeNodeName = false)
        {
            if (terminalRef.IsDummyEnd)
                return $"løs ende";

            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

            var rackName = GetRackName(terminalRef.RouteNodeId, terminalEquipment.Id);

            string? nodeName = null;

            if (includeNodeName)
            {
                nodeName = GetNodeName(terminalRef.RouteNodeId);

                if (nodeName != null)
                    nodeName += " ";
            }

            if (rackName != null)
                return $"{nodeName}{rackName} - {terminalEquipment.Name}";
            else
                return $"{nodeName}{terminalEquipment.Name}";
        }

        public string GetEquipmentWithStructureInfoString(IUtilityGraphTerminalRef terminalRef)
        {
            if (terminalRef.IsDummyEnd)
                return $"løs ende";

            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

            var terminalStructure = terminalRef.TerminalStructure(_utilityNetwork);

            var terminal = terminalRef.Terminal(_utilityNetwork);

            var rackName = GetRackName(terminalRef.RouteNodeId, terminalEquipment.Id);

            if (rackName != null)
                return $"{rackName}-{terminalEquipment.Name}-{terminalStructure.Position}-{terminal.Name}";
            else
                return $"{terminalEquipment.Name}-{terminalStructure.Position}-{terminal.Name}";
        }

        public string GetEquipmentStructureInfoString(IUtilityGraphTerminalRef terminalRef)
        {
            if (terminalRef.IsDummyEnd)
                return $"løs ende";

            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

            var terminalStructure = terminalRef.TerminalStructure(_utilityNetwork);

            var terminalStructureSpec = _terminalStructureSpecifications[terminalStructure.SpecificationId];

            string slotType = terminalStructureSpec.Category.ToLower().Contains("splice") ? "Bakke" : "Kort";

            return $"{slotType} {terminalStructure.Position}";
        }

        public string GetEquipmentTerminalInfoString(IUtilityGraphTerminalRef terminalRef)
        {
            if (terminalRef.IsDummyEnd)
                return $"løs ende";

            var terminalStructure = terminalRef.TerminalStructure(_utilityNetwork);

            var terminal = terminalRef.Terminal(_utilityNetwork);

            var terminalStructureSpec = _terminalStructureSpecifications[terminalStructure.SpecificationId];

            string pinType = terminalStructureSpec.Category.ToLower().Contains("splice") ? "Søm" : "Port";

            return $"{pinType} {terminal.Name}";
        }

        public string GetSpanEquipmentFullFiberCableString(SpanEquipment spanEquipment, int fiberNo)
        {
            int fiber = ((fiberNo - 1) % 12) + 1;
            int tube = ((fiberNo - 1) / 12) + 1;

            return $"{spanEquipment.Name} ({spanEquipment.SpanStructures.Length - 1}) Tube {tube} Fiber {fiber}";
        }

        public string GetSpanEquipmentTubeFiberString(SpanEquipment spanEquipment, int fiberNo)
        {
            int fiber = ((fiberNo - 1) % 12) + 1;
            int tube = ((fiberNo - 1) / 12) + 1;

            return $"Tube {tube} Fiber {fiber}";
        }
    }
}
