using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.QueryHandling
{
    public class GetSpanEquipmentSpecificationsQueryHandler
        : IQueryHandler<GetSpanEquipmentSpecifications, Result<LookupCollection<SpanEquipmentSpecification>>>
    {
        private readonly IEventStore _eventStore;        

        public GetSpanEquipmentSpecificationsQueryHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result<LookupCollection<SpanEquipmentSpecification>>> HandleAsync(GetSpanEquipmentSpecifications query)
        {
            var spanEquipmentSpecificationsProjection = _eventStore.Projections.Get<SpanEquipmentSpecificationsProjection>();

            return Task.FromResult(
                Result.Success<LookupCollection<SpanEquipmentSpecification>>(
                    spanEquipmentSpecificationsProjection.Specifications
                )
            );
        }
    }
}
