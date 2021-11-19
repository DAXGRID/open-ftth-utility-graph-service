using System;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record EquipmentConnectivityFaceConnectionInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string EndInfo { get; set; }
        public bool IsConnected { get; set; }
    }
}
