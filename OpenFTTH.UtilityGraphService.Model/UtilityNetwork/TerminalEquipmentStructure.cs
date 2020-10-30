using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// A structure part of a terminal equipment.
    /// I.e. a splice tray inside a splice closure, a card/slot in a switch, a port in a conduit closure etc.
    /// </summary>
    public struct TerminalEquipmentStructure : ITerminalEquipmentStructure
    {
        private ITerminalEquipmentStructure _parentStructure;

        public IReadOnlyList<ITerminalEquipmentStructure> ChildStructures { get; }

        public IReadOnlyList<ITerminalEquipmentNode> TerminalNodes { get; }
    }
}
