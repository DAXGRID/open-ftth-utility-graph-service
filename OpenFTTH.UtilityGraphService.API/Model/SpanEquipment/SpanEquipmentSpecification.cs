using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    public record SpanEquipmentSpecification : ISpanEquipmentSpecification
    {
        public Guid Id { get; }
        public string Kind { get;}
        public string Name { get; }
        public string Version { get; }

        public string? Color { get; init; }
        public string? Marking { get; init; }

        /// <summary>
        /// </summary>
        /// <param name="id">The specification id</param>
        /// <param name="kind">For what kind of equipment (MultiConduit, FiberCable) is the specification for?</param>
        /// <param name="name">Human readable name of the specification - i.e. Ø50 12x10/8</param>
        /// <param name="version">Since specifications are immutable, a version must always be provided</param>
        public SpanEquipmentSpecification(Guid id, string kind, string name, string version)
        {
            this.Id = id;
            this.Kind = kind;
            this.Name = name;
            this.Version = version;
        }
    }
}

