namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public record TerminalTemplate
    {
        public string Name { get; }
        public TerminalDirectionEnum Direction { get; }
        public bool IsPigtail { get; }
        public bool IsSplice { get; }
        public string? ConnectorType { get; init; }


    }
}
