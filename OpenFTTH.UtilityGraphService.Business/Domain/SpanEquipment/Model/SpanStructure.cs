using OpenFTTH.UtilityGraphService.API.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// SpanStructure implementation for internal service use
    /// </summary>
    public record SpanStructureImpl : SpanStructure
    {
        public SpanStructureImpl(Guid id, Guid specificationId) : base(id, specificationId)
        {
        }
    }
}
