using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public interface IUtilityGraphTerminalRef : IUtilityGraphElement
    {
        TerminalEquipment TerminalEquipment(UtilityNetworkProjection utilityNetwork);
        TerminalStructure TerminalStructure(UtilityNetworkProjection utilityNetwork);
        Terminal Terminal(UtilityNetworkProjection utilityNetwork);
        ushort StructureIndex { get; }
        ushort TerminalIndex { get; }
    }
}
