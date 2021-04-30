using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record PlaceSpanEquipmentInRouteNetwork : BaseCommand, ICommand<Result>
    {
        public Guid SpanEquipmentId { get; }
        public Guid SpanEquipmentSpecificationId { get; }
        public RouteNetworkInterest Interest { get; }
        public Guid? ManufacturerId { get; init; }
        public NamingInfo? NamingInfo { get; init; }
        public LifecycleInfo? LifecycleInfo { get; init; }
        public MarkingInfo? MarkingInfo { get; init; }

        public PlaceSpanEquipmentInRouteNetwork(Guid spanEquipmentId, Guid spanEquipmentSpecificationId, RouteNetworkInterest interest)
        {
            this.CmdId = Guid.NewGuid();
            this.Timestamp = DateTime.UtcNow;

            this.SpanEquipmentId = spanEquipmentId;
            this.SpanEquipmentSpecificationId = spanEquipmentSpecificationId;
            this.Interest = interest;
        }
    }
}
