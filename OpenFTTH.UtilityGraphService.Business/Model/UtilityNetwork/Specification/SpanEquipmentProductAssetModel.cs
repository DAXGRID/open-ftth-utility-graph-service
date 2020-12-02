using OpenFTTH.UtilityGraphService.Model.Asset;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.Specification
{
    public class SpanEquipmentProductAssetModel : IProductAssetModel
    {
        readonly Guid _mRID;
        readonly string _name;
        readonly string _version;
        readonly IManufacturer _manufacturer;
        readonly SpanEquipmentSpecification _specification;

        public SpanEquipmentProductAssetModel(Guid mRID, string name, string version, IManufacturer manufacturer, SpanEquipmentSpecification specification)
        {
            _mRID = mRID;
            _name = name;
            _version = version;
            _manufacturer = manufacturer;
            _specification = specification;
        }

        public ISpecification Specification => _specification;

        public IManufacturer Manufacturer => _manufacturer;

        public Guid MRID => _mRID;

        public string Name => _name;

        public string Version => _version;
    }
}
