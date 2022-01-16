using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.Trace;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Projections;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments.QueryHandling
{
    public class GetConnectivityFaceConnectionsQueryHandler
        : IQueryHandler<GetConnectivityFaceConnections, Result<List<EquipmentConnectivityFaceConnectionInfo>>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<RackSpecification> _rackSpecifications;
        private LookupCollection<TerminalStructureSpecification> _terminalStructureSpecifications;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;
        private LookupCollection<SpanEquipmentSpecification> _spanEquipmentSpecifications;

        public GetConnectivityFaceConnectionsQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result<List<EquipmentConnectivityFaceConnectionInfo>>> HandleAsync(GetConnectivityFaceConnections query)
        {
            _rackSpecifications = _eventStore.Projections.Get<RackSpecificationsProjection>().Specifications;
            _terminalStructureSpecifications = _eventStore.Projections.Get<TerminalStructureSpecificationsProjection>().Specifications;
            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
            _spanEquipmentSpecifications = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>().Specifications;

            if (_utilityNetwork.TryGetEquipment<TerminalEquipment>(query.spanOrTerminalEquipmentId, out var terminalEquipment))
            {
                // Find all terminal ends
                FindAllTerminalEnds(terminalEquipment, query.DirectionType);
            }
            else if (_utilityNetwork.TryGetEquipment<TerminalEquipment>(query.spanOrTerminalEquipmentId, out var spanEquipment))
            {

            }
            else
                return Task.FromResult(Result.Fail<List<EquipmentConnectivityFaceConnectionInfo>>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_EQUIPMENT_ID, $"Cannot find any span or terminal equipment with id: {query.spanOrTerminalEquipmentId}")));

        
            return Task.FromResult(Result.Ok(BuildConnectivityFaceConnections()));
        }

        private void FindAllTerminalEnds(TerminalEquipment terminalEquipment, ConnectivityDirectionEnum directionType)
        {
            
        }

        private List<EquipmentConnectivityFaceConnectionInfo> BuildConnectivityFaceConnections()
        {
            List<EquipmentConnectivityFaceConnectionInfo> connectivityFacesResult = new();

            /*
            connectivityFacesResult.Add(new EquipmentConnectivityFaceConnectionInfo()
            {
                Id = Guid.NewGuid(),
                Name = $"Rack 1 - LISA Tray 1 - Splice Pin {i}",
                EndInfo = $"GALARH OLT 1-1-{i % 2} <- KINA WDM 1-2-{i}",
                IsConnected = true
            });
            */


            return connectivityFacesResult;
        }

        private void FetchRelatedData(GetConnectivityFaceConnections query)
        {
            if (_utilityNetwork.TryGetEquipment<TerminalEquipment>(query.spanOrTerminalEquipmentId, out var terminalEquipment))
            {

            }
            else
            {

            }



        }


   
    }
}
