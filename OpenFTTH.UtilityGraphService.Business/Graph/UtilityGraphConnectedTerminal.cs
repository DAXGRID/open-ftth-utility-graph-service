using DAX.ObjectVersioning.Graph;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraphConnectedTerminal : GraphNode, IUtilityGraphElement
    {
        public Guid NodeOfInterestId { get; }

        public UtilityGraphConnectedTerminal(Guid id, Guid nodeOfInterestId) : base(id)
        {
            NodeOfInterestId = nodeOfInterestId;
        }

        public override string ToString()
        {
            return $"Terminal at route node: {NodeOfInterestId}";
        }
    }
}
