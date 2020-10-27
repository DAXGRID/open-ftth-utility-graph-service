using OpenFTTH.UtilityGraphService.Model.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.Asset
{
    public interface IProductAssetModel : IIdentifiedObject
    {
        public string Version { get; }
        public ISpecification Specification { get; }
        public IManufacturer Manufacturer { get; }
    }
}
