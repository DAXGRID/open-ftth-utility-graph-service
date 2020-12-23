using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.API.Model
{
    public interface IColorMarking
    {
        public string? Color { get; }
        public string? Marking { get; }
    }
}
