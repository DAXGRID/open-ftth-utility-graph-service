using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.CommandHandlers
{
    public class AddSpanStructureSpecificationCommandHandler : ICommandHandler<AddSpanStructureSpecification, Result>
    {
        private readonly IEventStore _eventStore;

        public AddSpanStructureSpecificationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(AddSpanStructureSpecification command)
        {
            var aggreate = _eventStore.Aggregates.Load<SpanStructureSpecificationsAR>(SpanStructureSpecificationsAR.UUID);

            aggreate.AddSpecification(command.Specification);

            _eventStore.Aggregates.Store(aggreate);

            return Task.FromResult(Result.Success());
        }
    }
}

  