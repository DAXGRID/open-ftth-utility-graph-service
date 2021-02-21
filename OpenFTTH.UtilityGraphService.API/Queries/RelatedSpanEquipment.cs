﻿using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
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

        public RelatedSpanEquipment(Guid id, Guid specificationId, RelatedSpanStructure rootStructure, WalkInfo walkInfo, SpanStructure[] spanStructures) : base(id, specificationId, walkInfo, spanStructures)
        {
            this._rootStructure = rootStructure;
        }
    }
}
