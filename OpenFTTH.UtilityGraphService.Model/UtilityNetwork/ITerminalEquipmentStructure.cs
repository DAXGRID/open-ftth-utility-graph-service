using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    public interface ITerminalEquipmentStructure
    {
        IReadOnlyList<ITerminalEquipmentStructure> ChildStructures { get; }
    }
}
