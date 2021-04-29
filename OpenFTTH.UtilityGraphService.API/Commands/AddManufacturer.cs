using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public class AddManufacturer : ICommand<Result>
    {
        public Manufacturer Manufacturer { get; }

        public AddManufacturer(Manufacturer manufacturer)
        {
            Manufacturer = manufacturer;
        }
    }
}
