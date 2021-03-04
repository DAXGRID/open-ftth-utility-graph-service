﻿using FluentResults;
using Newtonsoft.Json;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Projections;
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
        private readonly UtilityGraphProjection _utilityGraph;

        public GetEquipmentDetailsQueryHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
            _utilityGraph = _eventStore.Projections.Get<UtilityGraphProjection>();
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


            // Fetch span equipments by interest id
            foreach (var interestId in query.InterestIdsToQuery)
            {
                var equipmentLookupResult = _utilityGraph.GetEquipment(interestId);

                // Here we return a error result, because we're dealing with invalid interest ids provided by the client
                if (equipmentLookupResult.IsFailed)
                    return Task.FromResult(
                        Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_INTEREST_ID, spanEquipmentsByIdResult.Errors.First().Message))
                    ); 

                if (equipmentLookupResult.Value is SpanEquipment spanEquipment)
                    spanEquipmentsToReturn.Add(new SpanEquipmentWithRelatedInfo(spanEquipment));

                if (equipmentLookupResult.Value is NodeContainer nodeContainer)
                    nodeContainersToReturn.Add(nodeContainer);
            }

            var result = new GetEquipmentDetailsResult() {
                SpanEquipment = new LookupCollection<SpanEquipmentWithRelatedInfo>(spanEquipmentsToReturn),
                NodeContainers = new LookupCollection<NodeContainer>(nodeContainersToReturn)
            };
    
            return Task.FromResult(
                Result.Ok<GetEquipmentDetailsResult>(result)
            );
        }

        private Result<List<SpanEquipmentWithRelatedInfo>> GetSpanEquipmentsById(EquipmentIdList equipmentIdsToFetch)
        {
            List<SpanEquipmentWithRelatedInfo> result = new List<SpanEquipmentWithRelatedInfo>();

            foreach (var equipmentId in equipmentIdsToFetch)
            {
                var spanEquipmentLookpResult = _utilityGraph.GetEquipment(equipmentId);

                // Here we return a error result, because we're dealing with invalid equipment ids provided by the client
                if (spanEquipmentLookpResult.IsFailed)
                    return Result.Fail(spanEquipmentLookpResult.Errors.First());

                result.Add(new SpanEquipmentWithRelatedInfo((SpanEquipment)spanEquipmentLookpResult.Value));
            }

            return Result.Ok<List<SpanEquipmentWithRelatedInfo>>(result);
        }
    }
}
