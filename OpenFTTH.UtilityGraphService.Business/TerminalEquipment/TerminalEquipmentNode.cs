using DAX.ObjectVersioning.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// A node (acting as some kind of terminal) belonging to a terminal equipment.
    /// </summary>
    public class TerminalEquipmentNode : GraphNode, ITerminalEquipmentNode
    {
        private readonly TerminalEquipment _terminalEquipment;
        private readonly Int16 _parentStructureIndex;

        public TerminalEquipmentNode(Guid mRID, TerminalEquipment terminalEquipment, Int16 parentStructureIndex) : base(mRID)
        {
            _terminalEquipment = terminalEquipment;
            _parentStructureIndex = parentStructureIndex;
        }

        public Guid Id 
            => base.Id;

        public string Name 
            => throw new NotImplementedException();

        public ITerminalEquipment ParentEquipment 
            => _terminalEquipment;

        public ITerminalEquipmentStructure ParentStructure 
            => _terminalEquipment.GetStructureByIndexKey(_parentStructureIndex);
        
    }
}



