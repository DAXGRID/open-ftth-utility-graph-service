﻿using FluentResults;
using Newtonsoft.Json;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
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

        public GetEquipmentDetailsQueryHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result<GetEquipmentDetailsResult>> HandleAsync(GetEquipmentDetails query)
        {
            // Get route elements
            if (query.EquipmentIdsToQuery.Count > 0 && query.InterestIdsToQuery.Count == 0)
            {
                return QueryByEquipmentIds(query);
            }
            else if (query.InterestIdsToQuery.Count > 0 && query.EquipmentIdsToQuery.Count == 0)
            {
                throw new ApplicationException("Not Implemented");
            }
            else
            {
                if (query.InterestIdsToQuery.Count > 0 && query.EquipmentIdsToQuery.Count > 0)
                    return Task.FromResult(Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_CANT_QUERY_BY_INTEREST_AND_EQUIPMENT_ID_AT_THE_SAME_TIME, "Invalid query. Cannot query by equipment ids and interest ids at the same time.")));
                else if (query.InterestIdsToQuery.Count == 0 && query.EquipmentIdsToQuery.Count == 0)
                    return Task.FromResult(Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_NO_INTEREST_OR_EQUPMENT_IDS_SPECIFIED, "Invalid query. Neither route network element ids or interest ids specified. Therefore nothing to query.")));
                else
                    throw new ApplicationException("Unexpected combination of query arguments in GetRouteNetworkDetails query:\r\n" + JsonConvert.SerializeObject(query));
            }
        }

        private Task<Result<GetEquipmentDetailsResult>> QueryByEquipmentIds(GetEquipmentDetails query)
        {
            List<SpanEquipment> spanEquipmentsToReturn = new List<SpanEquipment>();

            var spanEquipmentProjection = _eventStore.Projections.Get<SpanEquipmentsProjection>();

            foreach (var equipmentId in query.EquipmentIdsToQuery)
            {
                var spanEquipmentLookpResult = spanEquipmentProjection.GetEquipment(equipmentId);

                // Here we return a error result, because we're dealing with invalid interest ids provided by the client
                if (spanEquipmentLookpResult.IsFailed)
                    return Task.FromResult(
                        Result.Fail<GetEquipmentDetailsResult>(new GetEquipmentDetailsError(GetEquipmentDetailsErrorCodes.INVALID_QUERY_ARGUMENT_ERROR_LOOKING_UP_SPECIFIED_EQUIPMENT_BY_ID, $"Error looking up equipment by id: {equipmentId}")).
                        WithError(spanEquipmentLookpResult.Errors.First())
                    );

                spanEquipmentsToReturn.Add(spanEquipmentLookpResult.Value);
            }

            var result = new GetEquipmentDetailsResult(spanEquipmentsToReturn.ToArray());
    
            return Task.FromResult(
                Result.Ok<GetEquipmentDetailsResult>(result)
            );
        }


    }
}
