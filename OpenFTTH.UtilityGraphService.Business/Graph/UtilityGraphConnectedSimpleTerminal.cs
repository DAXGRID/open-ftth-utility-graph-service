using DAX.ObjectVersioning.Graph;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public class UtilityGraphConnectedSimpleTerminal : GraphNode, IUtilityGraphTerminalRef
    {
        public Guid NodeOfInterestId { get; }

        public ushort StructureIndex => throw new NotImplementedException();

        public ushort TerminalIndex => throw new NotImplementedException();

        public UtilityGraphConnectedSimpleTerminal(Guid id, Guid nodeOfInterestId) : base(id)
        {
            NodeOfInterestId = nodeOfInterestId;
        }

        public override string ToString()
        {
            return $"Terminal at route node: {NodeOfInterestId}";
        }

        public TerminalEquipment TerminalEquipment(UtilityNetworkProjection utilityNetwork)
        {
            throw new NotImplementedException();
        }

        public TerminalStructure TerminalStructure(UtilityNetworkProjection utilityNetwork)
        {
            throw new NotImplementedException();
        }

        public Terminal Terminal(UtilityNetworkProjection utilityNetwork)
        {
            throw new NotImplementedException();
        }
    }
}
