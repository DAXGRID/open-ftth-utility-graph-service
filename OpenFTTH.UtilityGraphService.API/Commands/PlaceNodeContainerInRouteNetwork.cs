using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.RouteNetwork.API.Model;
using System;

namespace OpenFTTH.UtilityGraphService.API.Commands
{
    public record PlaceNodeContainerInRouteNetwork : ICommand<Result>
    {
        public Guid NodeContainerId { get; }
        public Guid NodeContainerSpecificationId { get; }
        public RouteNetworkInterest NodeOfInterest { get; }
        public Guid? ManufacturerId { get; init; }

        public PlaceNodeContainerInRouteNetwork(Guid nodeContainerId, Guid nodeContainerSpecificationId, RouteNetworkInterest nodeOfInterest)
        {
            NodeContainerId = nodeContainerId;
            NodeContainerSpecificationId = nodeContainerSpecificationId;
            NodeOfInterest = nodeOfInterest;
        }
    }
}
