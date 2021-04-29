﻿using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AddSpanStructureSpecification : BaseCommand, ICommand<Result>
    {
        public SpanStructureSpecification Specification { get; }

        public AddSpanStructureSpecification(SpanStructureSpecification specification)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            Specification = specification;
        }
    }
}
