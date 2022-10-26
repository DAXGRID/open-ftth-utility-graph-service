﻿using FluentResults;
using OpenFTTH.Address.API.Model;
using OpenFTTH.Address.API.Queries;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.Outage;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.Outage.QueryHandlers
{
    public class GetOutageViewQueryHandler : IQueryHandler<GetOutageView, Result<OutageViewNode>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<SpanEquipmentSpecification> _spanEquipmentSpecifications;
        private LookupCollection<SpanStructureSpecification> _spanStructureSpecifications;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;


        public GetOutageViewQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result<OutageViewNode>> HandleAsync(GetOutageView query)
        {
     
            var networkInterestsResult = GetRouteNetworkElementEquipmentOfInterest(query.RouteNetworkElementId);

            if (networkInterestsResult.IsFailed)
                return Task.FromResult(Result.Fail<OutageViewNode>(networkInterestsResult.Errors.First()));

            if (networkInterestsResult.Value.IsNode)
            {
                return Task.FromResult(Result.Ok(GetOutageViewForRouteNode(networkInterestsResult.Value)));
            }
            else
            {
                return Task.FromResult(Result.Ok(GetOutageViewForRouteSegment(networkInterestsResult.Value)));
            }
        }

        private OutageViewNode GetOutageViewForRouteNode(OutageProcessingState processingState)
        {
            OutageViewNode rootNode = new OutageViewNode(Guid.NewGuid(), "{OutageViewRouteNode}");

            return rootNode;
        }

        private OutageViewNode GetOutageViewForRouteSegment(OutageProcessingState processingState)
        {
            _spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            _spanStructureSpecifications = _eventStore.Projections.Get<SpanStructureSpecificationsProjection>().Specifications;

            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;


            OutageViewNode rootNode = new OutageViewNode(Guid.NewGuid(), "{OutageViewRouteSegment}");

            if (processingState.SpanEquipments == null)
            {
                rootNode.Description = "{OutageViewNoRelatedEquipmentsInRouteSegment}";
                return rootNode;
            }
            
            // Add conduits and related cables
            foreach (var outerConduit in processingState.SpanEquipments.Where(s => !s.IsCable))
            {
                var conduitSpecification = _spanEquipmentSpecifications[outerConduit.SpecificationId];

                var outerConduitNode = new OutageViewNode(Guid.NewGuid(), GetOuterConduitLabel(outerConduit, conduitSpecification));

                // If conduit is a multi conduit, then add each sub conduit
                if (conduitSpecification.IsMultiLevel)
                {
                    for (int i = 1; i < outerConduit.SpanStructures.Length; i++)
                    {
                        var innerConduit = outerConduit.SpanStructures[i];

                        var innerConduitSpecification = _spanStructureSpecifications[innerConduit.SpecificationId];

                        var innerConduitNode = new OutageViewNode(Guid.NewGuid(), GetInnerConduitLabel(innerConduit, innerConduitSpecification));

                        outerConduitNode.AddNode(innerConduitNode);

                        AddRelatedCables(processingState, innerConduitNode, innerConduit);

                    }
                }


                rootNode.AddNode(outerConduitNode);

            }


            // Add stand alone cables
            foreach (var cable in processingState.SpanEquipments.Where(s => s.IsCable))
            {
                if (!processingState.CableProcessed.Contains(cable.Id))
                {
                    var cableSpecification = _spanEquipmentSpecifications[cable.SpecificationId];

                    AddCable(processingState,rootNode, cable, cableSpecification);
                    processingState.CableProcessed.Add(cable.Id);
                }
            }


            AddAddressInformationToInstallations(processingState);

            return rootNode;
        }

        private void AddAddressInformationToInstallations(OutageProcessingState processingState)
        {
            HashSet<Guid> adresseIdsToFetch = new();

            foreach (var installation in processingState.InstallationNodes)
            {
                var installationAddressId = GetAddressIdFromTerminalEquipment(installation.Item2);

                if (installationAddressId != null)
                {
                    adresseIdsToFetch.Add(installationAddressId.Value);
                }
            }

            if (adresseIdsToFetch.Count > 0)
            {
                var addresses = GatherAddressInformation(adresseIdsToFetch);

                foreach (var installation in processingState.InstallationNodes)
                {
                    var installationAddressId = GetAddressIdFromTerminalEquipment(installation.Item2);

                    if (installationAddressId != null && addresses.ContainsKey(installationAddressId.Value))
                    {
                        installation.Item1.Description = addresses[installationAddressId.Value];
                    }
                }
            }
        }

        private Guid? GetAddressIdFromTerminalEquipment(TerminalEquipment terminalEquipment)
        {
            if (terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.UnitAddressId != null)
                return terminalEquipment.AddressInfo.UnitAddressId.Value;
            else if (terminalEquipment.AddressInfo != null && terminalEquipment.AddressInfo.AccessAddressId != null)
                return terminalEquipment.AddressInfo.AccessAddressId.Value;

            return null;
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


        private void AddRelatedCables(OutageProcessingState processingState, OutageViewNode innerConduitNode, SpanStructure innerConduit)
        {
            // Find related cables
            foreach (var spanSegment in innerConduit.SpanSegments)
            {
                if (_utilityNetwork.RelatedCablesByConduitSegmentId.ContainsKey(spanSegment.Id))
                {
                    foreach (var relatedCableId in _utilityNetwork.RelatedCablesByConduitSegmentId[spanSegment.Id])
                    {
                        if (_utilityNetwork.TryGetEquipment<SpanEquipment>(relatedCableId, out var cable))
                        {
                            var cableSpecification = _spanEquipmentSpecifications[cable.SpecificationId];

                            // Make sure that cable pass route element of interest
                            if (processingState.SpanEquipments.ContainsKey(cable.Id))
                            {
                                if (!processingState.CableProcessed.Contains(cable.Id))
                                {
                                    AddCable(processingState, innerConduitNode, cable, cableSpecification);
                                    processingState.CableProcessed.Add(cable.Id);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddCable(OutageProcessingState processingState, OutageViewNode parentNode, SpanEquipment cable, SpanEquipmentSpecification cableSpecification)
        {
            var cableNode = new OutageViewNode(Guid.NewGuid(), GetCableLabel(cable, cableSpecification));
            parentNode.AddNode(cableNode);

            // Trace all fibers to find eventually customers
            for (int fiberNumber = 1; fiberNumber < cable.SpanStructures.Count(); fiberNumber++)
            {
                var fiberStructure = cable.SpanStructures[fiberNumber];

                var installationEquipments = SearchForCustomerTerminationEquipmentInCircuit(fiberStructure.SpanSegments.First().Id);

                if (installationEquipments.Count > 0)
                {
                    // First add fiber node
                    var fiberNode = new OutageViewNode(Guid.NewGuid(), $"{{FiberNumber}}" + fiberNumber);
                    cableNode.AddNode(fiberNode);

                    // Now add all installations
                    foreach (var installationTerminalEquipment in installationEquipments)
                    {
                        var installationNode = new OutageViewNode(Guid.NewGuid(), installationTerminalEquipment.Name == null ? "NA" : installationTerminalEquipment.Name) { Value = installationTerminalEquipment.Name };
                        fiberNode.AddNode(installationNode);
                        processingState.InstallationNodes.Add((installationNode, installationTerminalEquipment));
                    }
                }
            }
        }

        private List<TerminalEquipment> SearchForCustomerTerminationEquipmentInCircuit(Guid fiberNetworkGraphElementId)
        {
            List<TerminalEquipment> result = new();

           var traceResult = _utilityNetwork.Graph.AdvancedTrace(fiberNetworkGraphElementId);

            if (traceResult != null && traceResult.All.Count > 0)
            {
                foreach (var trace in traceResult.All)
                {
                    if (trace is IUtilityGraphTerminalRef)
                    {
                        var terminalRef = (IUtilityGraphTerminalRef)trace;

                        if (!terminalRef.IsDummyEnd)
                        {
                            var terminalEquipment = terminalRef.TerminalEquipment(_utilityNetwork);

                            var terminalEquipmentSpecification = _terminalEquipmentSpecifications[terminalEquipment.SpecificationId];

                            if (terminalEquipmentSpecification.IsCustomerTermination)
                            {
                                result.Add(terminalEquipment);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private string GetCableLabel(SpanEquipment cable, SpanEquipmentSpecification cableSpecification)
        {
            return $"{cableSpecification.Name}";
        }

        private string GetInnerConduitLabel(SpanStructure innerConduit, SpanStructureSpecification innerConduitSpecification)
        {
            return $"{{InnerConduit}} {innerConduit.Position} ({{{innerConduitSpecification.Color}}})";
        }

        private string GetOuterConduitLabel(SpanEquipmentWithRelatedInfo outerConduit, SpanEquipmentSpecification spanEquipmentSpecification)
        {
            var label = spanEquipmentSpecification.Name;

            if (outerConduit.MarkingInfo != null && outerConduit.MarkingInfo.MarkingColor != null)
                label += $"({outerConduit.MarkingInfo.MarkingColor})";

            return label;
        }

        public Result<OutageProcessingState> GetRouteNetworkElementEquipmentOfInterest(Guid routeNetworkElementId)
        {
            // Query all interests related to route network element
            var routeNetworkInterestQuery = new GetRouteNetworkDetails(new RouteNetworkElementIdList() { routeNetworkElementId })
            {
                RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementAndInterestObjects
            };

            Result<GetRouteNetworkDetailsResult> interestsQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(routeNetworkInterestQuery).Result;

            if (interestsQueryResult.IsFailed)
                return Result.Fail(interestsQueryResult.Errors.First());

            OutageProcessingState result = new OutageProcessingState(interestsQueryResult.Value.RouteNetworkElements[routeNetworkElementId].Kind == RouteNetworkElementKindEnum.RouteNode);

            if (interestsQueryResult.Value.Interests == null)
                return Result.Ok(result);

            if (interestsQueryResult.Value.Interests.Count == 0)
                return Result.Ok(result);

            // Find equipments by interest ids
            var interestIdList = new InterestIdList();
            interestIdList.AddRange(interestsQueryResult.Value.Interests.Select(r => r.Id));

            var equipmentQueryResult = _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(interestIdList)
                {
                    EquipmentDetailsFilter = new EquipmentDetailsFilterOptions() { IncludeRouteNetworkTrace = false }
                }
            ).Result;

            if (equipmentQueryResult.IsFailed)
                return Result.Fail(equipmentQueryResult.Errors.First());
          

            if (equipmentQueryResult.Value.SpanEquipment != null)
            {
                result.SpanEquipments = equipmentQueryResult.Value.SpanEquipment;
            }

            if (equipmentQueryResult.Value.NodeContainers != null && equipmentQueryResult.Value.NodeContainers.Count == 1)
            {
                result.NodeContainer = equipmentQueryResult.Value.NodeContainers.First();
            }


            return Result.Ok(result);
        }

        public class OutageProcessingState
        {
            public bool IsNode { get; }
            public NodeContainer? NodeContainer { get; set; }
            public LookupCollection<SpanEquipmentWithRelatedInfo> SpanEquipments { get; set; }

            public HashSet<Guid> CableProcessed = new();

            public List<(OutageViewNode,TerminalEquipment)> InstallationNodes = new();

            public OutageProcessingState(bool isNode)
            {
                IsNode = isNode;
                SpanEquipments = new LookupCollection<SpanEquipmentWithRelatedInfo>();
            }
        }

        public class InstallationInfo
        {

        }
    }
}
