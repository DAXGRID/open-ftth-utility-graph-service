using OpenFTTH.UtilityGraphService.Model.RouteNetwork;
using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// The whole thing should be treated as an immutable structure.
    /// </summary>
    public class SpanEquipment
    {
        private IWalkOfInterest walkOfInterest;
        private IRouteNode[] _routeNodeIndex;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<SpanStructure> SpanStructures{ get; set; }
    }
}
