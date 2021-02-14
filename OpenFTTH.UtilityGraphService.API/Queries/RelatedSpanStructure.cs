using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    /// <summary>
    /// Used to represent span structure information related to a specific route network element.
    /// </summary>
    public record RelatedSpanStructure : SpanStructure
    {
        public RelatedSpanStructure(Guid id, Guid specificationId, SpanSegment[] spanSegments) : base(id, specificationId, spanSegments)
        {
        }
    }
}
