using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.Core
{
    public interface IIdentifiedObject
    {
        public Guid MRID { get; }
        public string Name { get; }
    }
}
