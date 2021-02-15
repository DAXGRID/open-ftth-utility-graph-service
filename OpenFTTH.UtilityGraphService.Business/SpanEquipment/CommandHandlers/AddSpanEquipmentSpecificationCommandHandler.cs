using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Projections;
using System;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.CommandHandlers
{
    public class AddSpanEquipmentSpecificationCommandHandler : ICommandHandler<AddSpanEquipmentSpecification, Result>
    {
        private readonly IEventStore _eventStore;

        public AddSpanEquipmentSpecificationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(AddSpanEquipmentSpecification command)
        {
            var aggreate = _eventStore.Aggregates.Load<SpanEquipmentSpecificationsAR>(SpanEquipmentSpecificationsAR.UUID);

            var spanStructureSpecifications = _eventStore.Projections.Get<SpanStructureSpecificationsProjection>().Specifications;

            try
            {
                aggreate.AddSpecification(command.Specification, spanStructureSpecifications);
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

  