using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record PlaceNodeContainerRouteNetwork : ICommand<Result>
    {
        public Guid NodeContainerId { get; }
        public Guid NodeContainerSpecificationId { get; }
        public RouteNetworkInterest NodeOfInterest { get; }
        public Guid? ManufacturerId { get; init; }

        public PlaceNodeContainerRouteNetwork(Guid nodeContainerId, Guid nodeContainerSpecificationId, RouteNetworkInterest nodeOfInterest)
        {
            NodeContainerId = nodeContainerId;
            NodeContainerSpecificationId = nodeContainerSpecificationId;
            NodeOfInterest = nodeOfInterest;
        }
    }
}
