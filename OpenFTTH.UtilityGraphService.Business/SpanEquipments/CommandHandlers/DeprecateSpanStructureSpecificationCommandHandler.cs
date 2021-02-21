using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class DeprecateSpanStructureSpecificationCommandHandler : ICommandHandler<DeprecateSpanStructureSpecification, Result>
    {
        private readonly IEventStore _eventStore;

        public DeprecateSpanStructureSpecificationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(DeprecateSpanStructureSpecification command)
        {
            var aggreate = _eventStore.Aggregates.Load<SpanStructureSpecificationsAR>(SpanStructureSpecificationsAR.UUID);

            aggreate.DeprecatedSpecification(command.SpanStructureSpecificationId);

            _eventStore.Aggregates.Store(aggreate);

            return Task.FromResult(Result.Success());
        }
    }
}

  