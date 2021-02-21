using CSharpFunctionalExtensions;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments
{
    /// <summary>
    /// The Span Equipment is used to model conduits and cables in the route network.
    /// Equipment that spans multiple route nodes and one or more route segments should be 
    /// modelled using the span equipment concept.
    /// </summary>
    public class SpanEquipmentAR : AggregateBase
    {
        private NamingInfo? NamingInfo { get; }
        private MarkingInfo? MarkingInfo { get; }

        public SpanEquipmentAR()
        {
            Register<SpanEquipmentPlacedInRouteNetwork>(Apply);
        }

        public Result PlaceSpanEquipmentInRouteNetwork(Guid spanEquipmentId, SpanEquipmentSpecification spanEquipmentSpecification, NamingInfo? namingInfo, MarkingInfo? markingInfo)
        {
            this.Id = spanEquipmentId;

            return Result.Success();
        }

        private void Apply(SpanEquipmentPlacedInRouteNetwork obj)
        {
            throw new NotImplementedException();
        }

       
    }
}
