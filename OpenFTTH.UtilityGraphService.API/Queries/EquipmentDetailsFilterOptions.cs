namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public record EquipmentDetailsFilterOptions
    {
        public bool IncludeSpanTrace { get; init; }

        public EquipmentDetailsFilterOptions()
        {
        }
    }
}
