using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.UtilityNetwork
{
    /// <summary>
    /// A concrete placeable piece of equipment that facilitates connectivity by means of graph nodes that can be connected to graph edges (modelled by means of span equipments).
    /// It is possible to model a composite terminal equipment by means of nested TerminalEquipmentStructures.
    /// Notice that the TerminalEquipment itself is a TerminalEquipmentStructure.
    /// </summary>
    public class TerminalEquipment : ITerminalEquipment, ITerminalEquipmentStructure
    {
        private IReadOnlyList<ITerminalEquipmentStructure>? _childStructures = null;

        private readonly Dictionary<Int16, ITerminalEquipmentStructure> _structureIndex = new Dictionary<short, ITerminalEquipmentStructure>();
        public IReadOnlyList<ITerminalEquipmentStructure>? ChildStructures {
            get { return _childStructures; }
            init { _childStructures = value; }
        }


        /// <summary>
        /// Get structure with the given index key
        /// </summary>
        /// <param name="structureIndexKey"></param>
        /// <returns></returns>
        public ITerminalEquipmentStructure GetStructureByIndexKey(Int16 structureIndexKey)
        {
            if (_structureIndex.Count <= structureIndexKey)
                throw new ArgumentException("Structure index out of bounds. Index data from caller must be corrupted or invalid.");

            return _structureIndex[structureIndexKey];
        }
    }
}
