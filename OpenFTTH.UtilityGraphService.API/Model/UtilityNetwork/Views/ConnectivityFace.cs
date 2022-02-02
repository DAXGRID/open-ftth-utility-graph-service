using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record ConnectivityFace
    {
        public ConnectivityDirectionEnum DirectionType { get; set; }
        public string DirectionName { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public ConnectivityEquipmentKindEnum EquipmentKind { get; set; }
    }
}
