using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record AddManufacturer : BaseCommand, ICommand<Result>
    {
        public Manufacturer Manufacturer { get; }

        public AddManufacturer(Manufacturer manufacturer)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            Manufacturer = manufacturer;
        }
    }
}
