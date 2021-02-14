using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public abstract record SpanEquipment 
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

        public abstract SpanEquipmentSpecification Specification { get; }

        public abstract SpanStructure RootStructure { get; }
    }
}
