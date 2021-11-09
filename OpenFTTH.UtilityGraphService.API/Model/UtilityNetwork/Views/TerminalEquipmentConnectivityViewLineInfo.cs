namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    /// <summary>
    /// Represents a line for display in a terminal equipment connectivity view
    /// </summary>
    public record TerminalEquipmentConnectivityViewLineInfo
    {
        public string ConnectorSymbol { get; init; }

        public TerminalEquipmentConnectivityViewEndInfo? A { get; init; }
        public TerminalEquipmentConnectivityViewEndInfo? Z { get; init; }

        public TerminalEquipmentConnectivityViewLineInfo(string connectorSymbol)
        {
            ConnectorSymbol = connectorSymbol;
        }
    }
}
