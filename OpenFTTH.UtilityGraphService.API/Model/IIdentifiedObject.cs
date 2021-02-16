using System;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    public interface IIdentifiedObject
    {
        public Guid Id { get; }

        /// <summary>
        /// I.e. a conduit or chassis number. Some utilities like to give their equipment numbers.
        /// Others do not. Therefore the property is nullable.
        /// </summary>
        public string? Name { get; }


        /// <summary>
        /// AKA long name
        /// </summary>
        public string? Description { get; }
    }
}
