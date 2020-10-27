using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.Specification
{
    /// <summary>
    /// Used as part of a span equipment specification to specify how spans (conduits, fibers etc.) are arranged in a hierarchical containment structure inside an equipment type.
    /// </summary>
    public struct SpanStructure
    {
        readonly string _spanClassType;
        readonly bool _isPathwayForOtherEquipment;
        readonly string? _name;
        readonly string? _color;
        readonly SpanStructure[]? _children;

        public SpanStructure(string spanClassType, bool pathwayForOtherEquipment, string? name = null, string? color = null, SpanStructure[]? childSpanStructures = null)
        {
            _spanClassType = spanClassType;
            _isPathwayForOtherEquipment = pathwayForOtherEquipment;
            _name = name;
            _color = color;
            _children = childSpanStructures;
        }

        /// <summary>
        /// Mandatory property telling which class of span we're dealing with - i.e. OuterConduit, InnerConduit, FiberCableJacket, FiberCableTube, FiberCableFiber...
        /// </summary>
        public string SpanClassType => _spanClassType;

        /// <summary>
        /// Name of the specific span inside the equipment - i.e. Subconduit 1, Subconduit 2 etc.
        /// </summary>
        public string? Name => _name;

        /// <summary>
        /// If the span has a color, then specify it using this property.
        /// </summary>
        public string? Color => _color;

        /// <summary>
        /// True is this span can act as a pathway for other span equipments - i.e. a conduit that is a pathway for cables.
        /// </summary>
        public bool IsPathwayForOtherEquipment => _isPathwayForOtherEquipment;

        /// <summary>
        /// Child span structures.
        /// </summary>
        public SpanStructure[] Children => _children == null ? new SpanStructure[0] : _children;
    }
}
