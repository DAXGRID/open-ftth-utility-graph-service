namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record TerminalEquipmentConnectivityViewEndInfo
    {
        public TerminalEquipmentConnectivityViewTerminalInfo Terminal { get; init; }

        public string? ConnectedTo { get; init; }
        public string? End { get; init; }

        public TerminalEquipmentConnectivityViewEndInfo(TerminalEquipmentConnectivityViewTerminalInfo terminal)
        {
            Terminal = terminal;
        }
    }
}
