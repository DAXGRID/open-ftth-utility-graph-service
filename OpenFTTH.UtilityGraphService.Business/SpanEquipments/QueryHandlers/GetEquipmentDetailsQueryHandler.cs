﻿using FluentResults;
using Newtonsoft.Json;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Tracing;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.QueryHandlers.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.QueryHandlers
{
    public class GetEquipmentDetailsQueryHandler :
        IQueryHandler<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public GetEquipmentDetailsQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result<GetEquipmentDetailsResult>> HandleAsync(GetEquipmentDetails query)
        {
            if (query.EquipmentIdsToQuery.Count > 0 || query.InterestIdsToQuery.Count > 0)
            {
                return QueryByEquipmentOrInterestIds(query);
            }
            else
            {
                if (query.InterestIdsToQuery.Count == 0 && query.EquipmentIdsToQuery.Count == 0)
                    return Task.FromResult(Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_NO_INTEREST_OR_EQUPMENT_IDS_SPECIFIED, "Invalid query. No equipment or interest ids specified. Therefore nothing to query.")));
                else
                    throw new ApplicationException("Unexpected combination of query arguments in GetEquipmentDetails query:\r\n" + JsonConvert.SerializeObject(query));
            }
        }

        private Task<Result<GetEquipmentDetailsResult>> QueryByEquipmentOrInterestIds(GetEquipmentDetails query)
        {
            List<SpanEquipmentWithRelatedInfo> spanEquipmentsToReturn = new List<SpanEquipmentWithRelatedInfo>();
            List<NodeContainer> nodeContainersToReturn = new List<NodeContainer>();

            // Fetch span equipments by id
            var spanEquipmentsByIdResult = GetSpanEquipmentsById(query.EquipmentIdsToQuery);

            if (spanEquipmentsByIdResult.IsSuccess)
                spanEquipmentsToReturn.AddRange(spanEquipmentsByIdResult.Value);
            else
                Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_EQUIPMENT_ID, spanEquipmentsByIdResult.Errors.First().Message));

            // Get eventually single span segment request
            var traceThisSpanSegmentIdOnly = GetSingleSpanSegmentTraceOnlyIdIfSpecificed(query.EquipmentIdsToQuery);

            // Fetch equipment by interest id
            foreach (var interestId in query.InterestIdsToQuery)
            {
                if (!_utilityNetwork.TryGetEquipment<IEquipment>(interestId, out IEquipment equipment))
                {
                    return Task.FromResult(
                        Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_INTEREST_ID, $"Cannot find equipment with interest id: {interestId}"))
                    );
                }

                if (equipment is SpanEquipment spanEquipment)
                {
                    spanEquipmentsToReturn.Add(new SpanEquipmentWithRelatedInfo(spanEquipment));
                }

                if (equipment is NodeContainer nodeContainer)
                    nodeContainersToReturn.Add(nodeContainer);
            }

            var result = new GetEquipmentDetailsResult() {
                SpanEquipment = new LookupCollection<SpanEquipmentWithRelatedInfo>(spanEquipmentsToReturn),
                NodeContainers = new LookupCollection<NodeContainer>(nodeContainersToReturn),
                RouteNetworkTraces = query.EquipmentDetailsFilter.IncludeRouteNetworkTrace ? AddTraceRefsToSpanEquipments(spanEquipmentsToReturn, traceThisSpanSegmentIdOnly) : null
            };
    
            return Task.FromResult(
                Result.Ok<GetEquipmentDetailsResult>(result)
            );
        }

        private Guid? GetSingleSpanSegmentTraceOnlyIdIfSpecificed(EquipmentIdList equipmentIdsToQuery)
        {
            if (equipmentIdsToQuery.Count == 1)
            {
                var spanEquipmentOrSegmentId = equipmentIdsToQuery[0];

                if (_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanEquipmentOrSegmentId, out var _))
                    return spanEquipmentOrSegmentId;
            }

            return null;
        }

        private LookupCollection<RouteNetworkTrace> AddTraceRefsToSpanEquipments(List<SpanEquipmentWithRelatedInfo> spanEquipmentsToReturn, Guid? traceThisSpanSegmentIdOnly)
        {
            var traceBuilder = new RouteNetworkTraceResultBuilder(_queryDispatcher, _utilityNetwork);

            var traceInfo = traceBuilder.GetTraceInfo(spanEquipmentsToReturn, traceThisSpanSegmentIdOnly);

            if (traceInfo != null)
            {
                foreach (var spanEquipment in spanEquipmentsToReturn)
                {
                    spanEquipment.RouteNetworkTraceRefs = traceInfo.SpanSegmentRouteNetworkTraceRefsBySpanEquipmentId[spanEquipment.Id].ToArray();
                }

                return new LookupCollection<RouteNetworkTrace>(traceInfo.RouteNetworkTraces);
            }
            else
                return new LookupCollection<RouteNetworkTrace>();
        }

        private Result<List<SpanEquipmentWithRelatedInfo>> GetSpanEquipmentsById(EquipmentIdList equipmentIdsToFetch)
        {
            List<SpanEquipmentWithRelatedInfo> result = new List<SpanEquipmentWithRelatedInfo>();

            foreach (var equipmentId in equipmentIdsToFetch)
            {
                if (!_utilityNetwork.TryGetEquipment<SpanEquipment>(equipmentId, out var spanEquipment))
                    return Result.Fail(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_EQUIPMENT_ID, $"Cannot find equipment with equipment id: {equipmentId}"));

                result.Add(new SpanEquipmentWithRelatedInfo(spanEquipment));
            }

            return Result.Ok<List<SpanEquipmentWithRelatedInfo>>(result);
        }
    }
}
