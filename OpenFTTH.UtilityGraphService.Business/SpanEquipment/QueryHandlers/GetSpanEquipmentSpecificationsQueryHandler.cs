using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.API.Util;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.QueryHandling
{
    public class GetSpanEquipmentSpecificationsQueryHandler
        : IQueryHandler<GetSpanEquipmentSpecifications, Result<LookupCollection<SpanEquipmentSpecification>>>
    {
        private readonly SpanEquipmentSpecificationsProjection _spanEquipmentSpecificationsProjection;

        public GetSpanEquipmentSpecificationsQueryHandler(SpanEquipmentSpecificationsProjection spanEquipmentSpecificationsProjection)
        {
            _spanEquipmentSpecificationsProjection = spanEquipmentSpecificationsProjection;
        }

        public Task<Result<LookupCollection<SpanEquipmentSpecification>>> HandleAsync(GetSpanEquipmentSpecifications query)
        {
            return Task.FromResult(
                Result.Success<LookupCollection<SpanEquipmentSpecification>>(
                    _spanEquipmentSpecificationsProjection.Specifications
                )
            );
        }
    }
}
