﻿using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public class GetSpanEquipmentSpecifications : IQuery<Result<LookupCollection<SpanEquipmentSpecification>>> { };
}
