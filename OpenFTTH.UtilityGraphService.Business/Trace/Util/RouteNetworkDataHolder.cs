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

        public string GetFullEquipmentString(Guid routeNodeId, TerminalEquipment terminalEquipment, bool includeNodeName = false)
        {
            var rackName = GetRackName(routeNodeId, terminalEquipment.Id);

            string? nodeName = null;

            if (includeNodeName)
            {
                nodeName = GetNodeName(routeNodeId);

                if (nodeName != null)
                    nodeName += " - ";
            }

            if (rackName != null)
                return $"{nodeName}{rackName} - {GetEquipmentName(terminalEquipment)}";
            else
                return $"{nodeName}{GetEquipmentName(terminalEquipment)}";
        }

        public string GetCompactEquipmentWithTypeInfoString(Guid routeNodeId, TerminalEquipment terminalEquipment, bool includeNodeName = false)
        {
            var rackName = GetRackName(routeNodeId, terminalEquipment.Id);

            string? nodeName = null;

            if (includeNodeName)
            {
                nodeName = GetNodeName(routeNodeId);

                if (nodeName != null)
                    nodeName += " - ";
            }

            var terminalEquipmentSpecification = _terminalEquipmentSpecifications[terminalEquipment.SpecificationId];


            if (rackName != null)
            {
              
                if (IsTerminalEquipmentRackTray(terminalEquipment, terminalEquipmentSpecification))
                {
                    return $"{nodeName}{rackName} ({terminalEquipmentSpecification.ShortName})";
                }
                {
                    return $"{nodeName}{rackName} - {GetEquipmentName(terminalEquipment)} ({terminalEquipmentSpecification.ShortName})";
                }
            }
            else
            {
                return $"{nodeName}{GetEquipmentName(terminalEquipment)} ({terminalEquipmentSpecification.ShortName})";
            }
        }

        public string? GetEquipmentName(TerminalEquipment terminalEquipment)
        {
            // Single structure equipment
            if (terminalEquipment.TerminalStructures.Length == 1)
            {
                var terminal = terminalEquipment.TerminalStructures[0].Terminals[0];
                var terminalEquipmentSpecification = _terminalEquipmentSpecifications[terminalEquipment.SpecificationId];

                // If rack equipment where type is not put into name
                if (IsTerminalEquipmentRackTray(terminalEquipment, terminalEquipmentSpecification))
                {
                    if (terminal.IsSplice)
                        return "Bakke " + terminalEquipment.Name;
                    else
                        return "Kort " + terminalEquipment.Name;
                }
            }

            return terminalEquipment.Name;
        }

        public string GetEquipmentWithStructureInfoString(IUtilityGraphTerminalRef terminalRef)
        {
            if (terminalRef.IsDummyEnd)
                return $"løs ende";

            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

            var terminalStructure = terminalRef.TerminalStructure(_utilityNetwork);

            var terminal = terminalRef.Terminal(_utilityNetwork);

            var rackName = GetRackName(terminalRef.RouteNodeId, terminalEquipment.Id);

            string? terminalStructurePosition = null;

            if (terminalEquipment.TerminalStructures.Length > 1)
            {
                terminalStructurePosition = $"-{terminalStructure.Position}";
            }

            if (rackName != null)
                return $"{rackName}-{terminalEquipment.Name}{terminalStructurePosition}-{terminal.Name}";
            else
                return $"{terminalEquipment.Name}{terminalStructurePosition}-{terminal.Name}";
        }

        public string GetEquipmentStructureInfoString(IUtilityGraphTerminalRef terminalRef)
        {
            if (terminalRef.IsDummyEnd)
                return $"løs ende";

            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

            var terminalEquipmentSpecification = _terminalEquipmentSpecifications[terminalEquipment.SpecificationId];

            var terminalStructure = terminalRef.TerminalStructure(_utilityNetwork);

            var terminalStructureSpec = _terminalStructureSpecifications[terminalStructure.SpecificationId];

            // If rack equipment where type is not put into name
            if (IsTerminalEquipmentRackTray(terminalEquipment, terminalEquipmentSpecification))
            {
                return GetEquipmentName(terminalEquipment);
            }
            else
            {
                string slotType = terminalStructureSpec.Category.ToLower().Contains("splice") ? "Bakke" : "Kort";

                return $"{slotType} {terminalStructure.Position}";
            }
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


        private bool IsTerminalEquipmentRackTray(TerminalEquipment terminalEquipment, TerminalEquipmentSpecification terminalEquipmentSpecification)
        {
            // If rack equipment where type is not put into name
            if (terminalEquipmentSpecification.IsRackEquipment && int.TryParse(terminalEquipment.Name, out _))
                return true;
            else
                return false;
        }
    }
}
