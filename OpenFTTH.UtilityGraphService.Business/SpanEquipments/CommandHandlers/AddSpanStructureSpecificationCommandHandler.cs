﻿using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using System;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
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

            try
            {
                aggreate.AddSpecification(command.Specification);
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

  