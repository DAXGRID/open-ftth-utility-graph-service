using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    public interface ISpanStructure : IIdentifiedObject
    {
        public List<ISpanStructure>? ChildStructures { get; }
        public bool HasChildren { get; }
    }
}
