using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    /// <summary>
    /// Notice that base implementations of this abstract type can be found in both the API and Business module.
    /// </summary>
    public abstract record SpanEquipment : ISpanEquipment
    {
        public Guid Id { get; }
        
        public string? Name { get; init; }

        public Guid SpecificationId { get; }

        public bool IsComposite { get; }

        public SpanEquipment(Guid id, Guid specificationId)
        {
            this.Id = id;
            this.SpecificationId = specificationId;
        }

        public abstract ISpanEquipmentSpecification Specification { get; }

        public abstract ISpanStructure RootStructure { get; }
    }
}
