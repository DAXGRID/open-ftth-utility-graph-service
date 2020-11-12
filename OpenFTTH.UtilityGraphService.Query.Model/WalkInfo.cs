using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.QueryModel
{
    public class WalkInfo
    {
        public WalkInfo(Int32[] routeElements)
        {
            RouteElements = routeElements;
        }

        public Int32[] RouteElements { get; set; }
    }
}
