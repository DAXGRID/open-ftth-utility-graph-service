using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views
{
    public record EquipmentConnectivityFace
    {
        public string DirectionType { get; set; }
        public string DirectionName { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentKind { get; set; }
    }
}
