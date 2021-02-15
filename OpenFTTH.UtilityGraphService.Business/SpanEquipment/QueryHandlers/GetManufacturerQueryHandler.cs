using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.API.Util;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.QueryHandling
{
    public class GetManufacturerQueryHandler
        : IQueryHandler<GetManufacturer, Result<LookupCollection<Manufacturer>>>
    {
        private readonly IEventStore _eventStore;        

        public GetManufacturerQueryHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result<LookupCollection<Manufacturer>>> HandleAsync(GetManufacturer query)
        {
            var manufacturerProjection = _eventStore.Projections.Get<ManufacturerProjection>();

            return Task.FromResult(
                Result.Success<LookupCollection<Manufacturer>>(
                    manufacturerProjection.Manufacturer
                )
            );
        }
    }
}
