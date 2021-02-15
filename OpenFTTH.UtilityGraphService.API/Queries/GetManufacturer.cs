﻿using CSharpFunctionalExtensions;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Util;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public class GetManufacturer : IQuery<Result<LookupCollection<Manufacturer>>> { };
}
