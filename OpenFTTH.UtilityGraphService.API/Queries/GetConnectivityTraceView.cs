using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.API.Queries
{
    public class GetConnectivityTraceView : IQuery<Result<ConnectivityTraceView>> 
    { 
        public Guid? SpanSegmentId { get; }

        public Guid? TerminalId { get; }

        public GetConnectivityTraceView(Guid spanSegmentId)
        {
            this.SpanSegmentId = SpanSegmentId;
        }
    }
}
