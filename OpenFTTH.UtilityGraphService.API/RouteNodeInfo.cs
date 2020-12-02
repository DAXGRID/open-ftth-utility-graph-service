using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.QueryModel
{
    public record RouteNodeInfo
    {
        public RouteNodeInfo(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}
