using FluentResults;
using OpenFTTH.Address.API.Model;
using OpenFTTH.Address.API.Queries;
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
    public class RelatedDataHolder
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;
        private LookupCollection<TerminalStructureSpecification> _terminalStructureSpecifications;

        public Dictionary<Guid, RouteNetworkElement> RouteNetworkElementById { get; }
        public Dictionary<Guid, NodeContainer> NodeContainerById  { get; }
        public Dictionary<Guid, string> AddressStringById { get; }

        public RelatedDataHolder(IEventStore eventStore, UtilityNetworkProjection utilityNetwork, IQueryDispatcher queryDispatcher, IEnumerable<Guid> nodeOfInterestIds, HashSet<Guid>? addressIds = null)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = utilityNetwork;

            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
            _terminalStructureSpecifications = _eventStore.Projections.Get<TerminalStructureSpecificationsProjection>().Specifications;

            RouteNetworkElementById = GatherRouteNetworkElementInformation(nodeOfInterestIds);

            NodeContainerById = GatcherNodeContainerInformation(RouteNetworkElementById.Values.ToList());

            AddressStringById = GatherAddressInformation(addressIds);
        }

        private Dictionary<Guid, RouteNetworkElement> GatherRouteNetworkElementInformation(IEnumerable<Guid> nodeOfInterestIds)
        {
            if (nodeOfInterestIds.Count() == 0)
            {
                return new Dictionary<Guid, RouteNetworkElement>();
            }

            RouteNetworkElementIdList idList = new();
            idList.AddRange(nodeOfInterestIds);

            var routeNetworkQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
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

            return routeNetworkQueryResult.Value.RouteNetworkElements.ToDictionary(x => x.Id);
        }

        private Dictionary<Guid, NodeContainer> GatcherNodeContainerInformation(List<RouteNetworkElement> routeNetworkElements)
        {
            Dictionary<Guid, NodeContainer> result = new();

            // Get node containers
            foreach (var routeNetworkElement in RouteNetworkElementById.Values)
            {
                if (routeNetworkElement.InterestRelations != null)
                {
                    foreach (var interestRel in routeNetworkElement.InterestRelations)
                    {
                        if (_utilityNetwork.TryGetEquipment<NodeContainer>(interestRel.RefId, out var nodeContainer))
                        {
                            result.Add(routeNetworkElement.Id, nodeContainer);
                        }
                    }
                }
            }

            return result;
        }

        private Dictionary<Guid, string> GatherAddressInformation(HashSet<Guid>? addressIdsToQuery)
        {
            if (addressIdsToQuery == null)
                return new Dictionary<Guid, string>();

            var getAddressInfoQuery = new GetAddressInfo(addressIdsToQuery.ToArray());

            var addressResult = _queryDispatcher.HandleAsync<GetAddressInfo, Result<GetAddressInfoResult>>(getAddressInfoQuery).Result;

            Dictionary<Guid, string> result = new();

            if (addressResult.IsSuccess)
            {
                foreach (var addressHit in addressResult.Value.AddressHits)
                {
                    if (addressHit.RefClass == AddressEntityClass.UnitAddress)
                    {
                        var unitAddress = addressResult.Value.UnitAddresses[addressHit.RefId];
                        var accessAddress = addressResult.Value.AccessAddresses[unitAddress.AccessAddressId];

                        var addressStr = accessAddress.RoadName + " " + accessAddress.HouseNumber;

                        if (unitAddress.FloorName != null)
                            addressStr += (", " + unitAddress.FloorName);

                        if (unitAddress.SuitName != null)
                            addressStr += (" " + unitAddress.SuitName);

                        result.Add(addressHit.Key, addressStr);
                    }
                    else
                    {
                        var accessAddress = addressResult.Value.AccessAddresses[addressHit.RefId];

                        var addressStr = accessAddress.RoadName + " " + accessAddress.HouseNumber;

                        result.Add(addressHit.Key, addressStr);
                    }
                }
            }
            else
            {
                throw new ApplicationException($"Error calling address service from trace. Error: " + addressResult.Errors.First().Message);
            }

            return result;
        }

        public string? GetAddressString(Guid? addressId)
        {
            if (addressId == null || addressId.Value == Guid.Empty)
                return null;

            if (AddressStringById.ContainsKey(addressId.Value))
                return AddressStringById[addressId.Value];
            else
                return null;
        }

        public string? GetNodeName(Guid routeNodeId)
        {
            if (RouteNetworkElementById != null && RouteNetworkElementById.ContainsKey(routeNodeId))
            {
                var node = RouteNetworkElementById[routeNodeId];
                return node.Name;
            }

            return null;
        }

        public string? GetNodeOrEquipmentName(Guid routeNodeId, TerminalEquipment terminalEquipment)
        {
            if (RouteNetworkElementById != null && RouteNetworkElementById.ContainsKey(routeNodeId))
            {
                var nodeName = RouteNetworkElementById[routeNodeId].Name;

                if (!String.IsNullOrEmpty(nodeName))
                    return nodeName;
            }

            return terminalEquipment.Name;
        }

        public string? GetRackName(Guid routeNodeId, Guid terminalEquipmentId)
        {
            if (NodeContainerById.ContainsKey(routeNodeId))
            {
                var container = NodeContainerById[routeNodeId];

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

            // Prepare node name if available
            if (nodeName != null)
                nodeName += " ";

            // Prepare address info if available
            string? addressInfo = null;

            if (terminalEquipment.AddressInfo != null)
            {
                addressInfo = GetAddressString(GetTerminalEquipmentMostAccurateAddressId(terminalEquipment));

                if (addressInfo != null)
                    addressInfo = " (" + addressInfo + ")";
            }

            var equipmentName = GetEquipmentWithStructureInfoString(terminalRef);

            return $"{nodeName}{equipmentName}{addressInfo}";
        }

        public string GetFullEquipmentString(Guid routeNodeId, TerminalEquipment terminalEquipment, bool includeNodeName = false, bool includeAddressInfo = false)
        {
            var rackName = GetRackName(routeNodeId, terminalEquipment.Id);

            string? nodeName = null;

            if (includeNodeName)
            {
                nodeName = GetNodeName(routeNodeId);

                if (nodeName != null)
                    nodeName += " - ";
            }

            string? addressInfo = null;

            if (includeAddressInfo && terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.AccessAddressId != null)
            {
                addressInfo = GetAddressString(GetTerminalEquipmentMostAccurateAddressId(terminalEquipment));

                if (addressInfo != null)
                    addressInfo = " (" + addressInfo + ")";
            }

            if (rackName != null)
                return $"{nodeName}{rackName} - {GetEquipmentName(terminalEquipment)}{addressInfo}";
            else
                return $"{nodeName}{GetEquipmentName(terminalEquipment)}{addressInfo}";
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
                else
                {
                    return $"{nodeName}{rackName} - {GetEquipmentName(terminalEquipment)} ({terminalEquipmentSpecification.ShortName})";
                }
            }
            else
            {
                if (IsCustomerTermination(terminalEquipment, terminalEquipmentSpecification))
                {
                    return $"{terminalEquipmentSpecification.ShortName}";
                }
                else
                {
                    return $"{nodeName}{GetEquipmentName(terminalEquipment)} ({terminalEquipmentSpecification.ShortName})";
                }
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

        private Guid? GetTerminalEquipmentMostAccurateAddressId(TerminalEquipment terminalEquipment)
        {
            if (terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.UnitAddressId != null)
                return terminalEquipment.AddressInfo.UnitAddressId.Value;
            else if (terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.AccessAddressId != null)
                return terminalEquipment.AddressInfo.AccessAddressId.Value;

            return null;
        }

        private bool IsTerminalEquipmentRackTray(TerminalEquipment terminalEquipment, TerminalEquipmentSpecification terminalEquipmentSpecification)
        {
            // If rack equipment where type is not put into name
            if (terminalEquipmentSpecification.IsRackEquipment && int.TryParse(terminalEquipment.Name, out _))
                return true;
            else
                return false;
        }

        private bool IsCustomerTermination(TerminalEquipment terminalEquipment, TerminalEquipmentSpecification terminalEquipmentSpecification)
        {
            // TODO: Remove this after conversion fix
            if (terminalEquipmentSpecification.Category != null && (terminalEquipmentSpecification.Category == "Kundeterminering"))
                return true;

            if (terminalEquipmentSpecification.IsCustomerTermination)
                return true;
            else
                return false;


        }
    }
}
