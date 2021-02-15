using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using System;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.CommandHandlers
{
    public class AddManufacturerCommandHandler : ICommandHandler<AddManufacturer, Result>
    {
        private readonly IEventStore _eventStore;

        public AddManufacturerCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(AddManufacturer command)
        {
            var aggreate = _eventStore.Aggregates.Load<ManufacturerAR>(ManufacturerAR.UUID);

            try
            {
                aggreate.AddManufacturer(command.Manufacturer);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(Result.Failure(ex.Message));
            }

            _eventStore.Aggregates.Store(aggreate);

            return Task.FromResult(Result.Success());
        }
    }
}

  