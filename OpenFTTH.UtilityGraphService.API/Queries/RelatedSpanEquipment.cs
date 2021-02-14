using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;
using System.Runtime.Serialization;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    /// <summary>
    /// Used to represent span equipment information related to a specific route network element as part 
    /// of the GetEquipmentRelatedToRouteElementQueryResult data structure.
    /// </summary>
    public record RelatedSpanEquipment : SpanEquipment
    {
        private readonly RelatedSpanStructure _rootStructure;

        internal SpanEquipmentSpecification? _specification;

        public RelatedSpanEquipment(Guid id, Guid specificationId, RelatedSpanStructure rootStructure) : base(id, specificationId)
        {
            this._rootStructure = rootStructure;
        }

        public override RelatedSpanStructure RootStructure => _rootStructure;

        [IgnoreDataMember]
        public override SpanEquipmentSpecification Specification => _specification ?? new SpanEquipmentSpecification(Guid.NewGuid(), "run PopulateReferences IQueryResult", "notset", null);
    }
}
