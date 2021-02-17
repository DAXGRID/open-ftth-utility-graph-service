using OpenFTTH.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    public interface IEquipment : IIdentifiedObject
    {
        /// <summary>
        /// Convenience property telling if the equipment has a composite structure or not.
        /// 
        /// If true, there will be child structures underneath the root structure of the equipment - i.e.
        /// a multi conduit consisting of an outer conduit and inner conduits, or a fiber cable consisting 
        /// of a jacket, tubes, and fibres.
        /// 
        /// If false, there will only be a root structure - i.e. a simple conduit with no inner conduits.
        /// </summary>
        public bool IsComposite { get; }
    }
}
