using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment
{
    /// <summary>
    /// The Span Equipment is used to model conduits and cables in the route network.
    /// Equipment that spans multiple route nodes and one or more route segments should be 
    /// modelled using the span equipment concept.
    /// </summary>
    public class SpanEquipmentAR : AggregateBase
    {
        private Guid? ProductAssetModelRef { get; }
        private NamingInfo? NamingInfo { get; }
        private MarkingInfo? MarkingInfo { get; }

        public SpanEquipmentAR(Guid id, Guid walkOfInterestId, SpanEquipmentSpecificationsAR spanEquipmentSpecification, NamingInfo? namingInfo)
        {
            this.Id = id;
            this.NamingInfo = namingInfo;
        }
    }
}
