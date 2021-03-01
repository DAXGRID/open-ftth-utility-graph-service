using DAX.ObjectVersioning.Graph;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraphConnectedTerminal : GraphNode, IUtilityGraphElement
    {
        public UtilityGraphConnectedTerminal(Guid id) : base(id)
        {
        }
    }
}
