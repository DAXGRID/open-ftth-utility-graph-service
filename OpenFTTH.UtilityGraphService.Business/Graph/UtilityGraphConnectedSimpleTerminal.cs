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

        public Guid TerminalId => Id;

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
            if (utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(Id, out var terminalGraphElement))
            {
                return terminalGraphElement.TerminalEquipment(utilityNetwork);
            }

            throw new ApplicationException($"Cannot find terminal equipment by terminal id: {Id}. State corrupted!");
        }

        public TerminalStructure TerminalStructure(UtilityNetworkProjection utilityNetwork)
        {
            if (utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(Id, out var terminalGraphElement))
            {
                return terminalGraphElement.TerminalEquipment(utilityNetwork).TerminalStructures[terminalGraphElement.StructureIndex];
            }

            throw new ApplicationException($"Cannot find terminal equipment by terminal id: {Id}. State corrupted!");
        }

        public Terminal Terminal(UtilityNetworkProjection utilityNetwork)
        {
            if (utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(Id, out var terminalGraphElement))
            {
                return terminalGraphElement.TerminalEquipment(utilityNetwork).TerminalStructures[terminalGraphElement.StructureIndex].Terminals[terminalGraphElement.TerminalIndex];
            }

            throw new ApplicationException($"Cannot find terminal equipment by terminal id: {Id}. State corrupted!");
        }
    }
}
