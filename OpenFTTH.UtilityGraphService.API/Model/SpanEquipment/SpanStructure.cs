using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    public record SpanStructure : ISpanStructure
    {
        public Guid Id { get; }
        public string? Name { get; init; }
        public Guid SpecificationId { get; }
        public List<ISpanStructure>? ChildStructures { get; init; }
        public bool HasChildren => ChildStructures != null;
        
        public SpanStructure(Guid id, Guid specificationId)
        {
            Id = id;
            SpecificationId = specificationId;
        }
    }
}
