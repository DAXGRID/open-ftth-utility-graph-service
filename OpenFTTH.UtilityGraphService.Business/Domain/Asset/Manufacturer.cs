using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Model.Asset
{
    public class Manufacturer : IManufacturer
    {
        readonly Guid _mRID;
        readonly string _name;

        public Manufacturer(Guid mRID, string name)
        {
            _mRID = mRID;
            _name = name;
        }

        public Guid MRID => _mRID;

        public string Name => _name;
    }
}
