using OpenFTTH.UtilityGraphService.API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public override ISpanEquipmentSpecification Specification => _specification ?? new SpanEquipmentSpecification(Guid.NewGuid(), "run PopulateReferences IQueryResult", "notset", "notset");
    }
}
