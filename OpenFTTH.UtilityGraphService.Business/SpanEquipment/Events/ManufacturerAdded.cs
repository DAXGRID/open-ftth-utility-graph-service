using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events
{
    public record ManufacturerAdded
    {
        public Manufacturer Manufacturer { get; }

        public ManufacturerAdded(Manufacturer manufacturer)
        {
            Manufacturer = manufacturer;
        }
    }
}
