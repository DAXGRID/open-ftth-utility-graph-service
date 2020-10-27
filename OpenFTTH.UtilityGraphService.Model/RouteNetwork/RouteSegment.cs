using DAX.ObjectVersioning.Graph;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.RouteNetwork
{
    public class RouteSegment : GraphEdge, IRouteSegment
    {
        private readonly Envelope _envelope;
        public Envelope Envelope => _envelope;

        public RouteSegment(Guid id, RouteNode fromNode, RouteNode toNode, Envelope envelope) : base(id, fromNode, toNode)
        {
            _envelope = envelope;
        }
    }
}
