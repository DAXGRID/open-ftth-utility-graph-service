using System;

namespace OpenFTTH.UtilityGraphService.API.Model.Asset 
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

        public Guid Id => _mRID;

        public string Name => _name;
    }
}
