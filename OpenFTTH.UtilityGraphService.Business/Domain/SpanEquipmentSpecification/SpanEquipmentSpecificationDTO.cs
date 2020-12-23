using OpenFTTH.UtilityGraphService.Model.Asset;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.Specification
{
    /// <summary>
    /// Use to specify typical datasheet information of a span equipment that can be shared among specific asset product models
    /// </summary>
    public class SpanEquipmentSpecificationDTO : ISpecification
    {
        readonly Guid _mRID;
        readonly string _name;
        readonly string _version;
        readonly SpanStructureSpecification _structure;

        public SpanEquipmentSpecificationDTO(Guid mRID, string name, string version, SpanStructureSpecification structure)
        {
            _mRID = mRID;
            _name = name;
            _version = version;
            _structure = structure;
        }

        public Guid MRID => _mRID;

        public string Name => _name;
    }
}
