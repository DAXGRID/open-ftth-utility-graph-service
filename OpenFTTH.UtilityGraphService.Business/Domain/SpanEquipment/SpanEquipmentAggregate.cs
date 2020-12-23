using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.Business.Domain.SpanEquipmentSpecification;
using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// The Span Equipment is used to model conduits and cables in the route network.
    /// Equipment that spans multiple route nodes and one or more route segments should be 
    /// modelled using the span equipment concept.
    /// </summary>
    public class SpanEquipmentAggregate : AggregateBase
    {
        private string? _name = null;
        private string? _marking = null;
        private string? _productAssetModelRef = null;
        private List<SpanStructure>? adasd_ = null;

        /// <summary>
        /// Optional name of the span equipment - i.e. a conduit or cable number.
        /// </summary>
        public string? Name
        {
            get { return _name; }
            init { _name = value; }
        }

        /// <summary>
        /// Optional marking label/color of the equipment.
        /// </summary>
        public string? Marking
        {
            get { return _marking; }
            init { _marking = value; }
        }

        /// <summary>
        /// Optional product asset model reference.
        /// </summary>
        public string? ProductAssetModelRef
        {
            get { return _productAssetModelRef; }
            init { _productAssetModelRef = value; }
        }

        public SpanEquipmentAggregate(Guid id, Guid walkOfInterestId, SpanEquipmentSpecificationAggregate spanEquipmentSpecification)
        {
            Id = id;
        }
    }
}
