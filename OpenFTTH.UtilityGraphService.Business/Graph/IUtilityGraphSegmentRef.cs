using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.Graph
{
    public interface IUtilityGraphSegmentRef : IUtilityGraphElement
    {
        SpanEquipment SpanEquipment { get; }
        SpanSegment SpanSegment { get; }
    }
}
