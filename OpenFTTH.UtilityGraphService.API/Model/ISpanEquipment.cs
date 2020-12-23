using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    /// <summary>
    /// An equipment that spans one or more route segments - i.e. a conduit or cable.
    /// </summary>
    public interface ISpanEquipment : IEquipment
    {
        public ISpanEquipmentSpecification Specification { get; }
        public ISpanStructure RootStructure { get; }
    }
}
