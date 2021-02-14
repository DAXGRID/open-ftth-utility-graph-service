using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public class SpanStructureTemplate
    {
        public Guid SpanStructureSpecificationId { get; }
        public int Position { get; }
        public SpanStructureTemplate[] ChildTemplates { get; }

        public SpanStructureTemplate(Guid spanStructureSpecificationId, int position, SpanStructureTemplate[] childTemplates)
        {
            SpanStructureSpecificationId = spanStructureSpecificationId;
            Position = position;
            ChildTemplates = childTemplates;
        }
    }
}
