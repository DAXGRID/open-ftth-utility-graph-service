namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record TerminalEquipmentAZConnectivityViewEndInfo
    {
        public TerminalEquipmentAZConnectivityViewTerminalInfo Terminal { get; init; }

        public string? ConnectedTo { get; init; }
        public string? End { get; init; }

        public TerminalEquipmentAZConnectivityViewEndInfo(TerminalEquipmentAZConnectivityViewTerminalInfo terminal)
        {
            Terminal = terminal;
        }
    }
}
