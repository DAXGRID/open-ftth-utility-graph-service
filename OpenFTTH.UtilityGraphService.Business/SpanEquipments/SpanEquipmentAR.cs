using FluentResults;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Projections;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments
{
    /// <summary>
    /// The Span Equipment is used to model conduits and cables in the route network.
    /// Equipment that spans multiple route nodes and one or more route segments should be 
    /// modelled using the span equipment concept.
    /// </summary>
    public class SpanEquipmentAR : AggregateBase
    {
        private SpanEquipment? _spanEquipment;

        public SpanEquipmentAR()
        {
            Register<SpanEquipmentPlacedInRouteNetwork>(Apply);
            Register<SpanEquipmentAffixedToContainer>(Apply);
        }

        public Result PlaceSpanEquipmentInRouteNetwork(
            LookupCollection<SpanEquipment> spanEquipments,
            LookupCollection<SpanEquipmentSpecification> spanEquipmentSpecifications,
            Guid spanEquipmentId, 
            Guid spanEquipmentSpecificationId,
            RouteNetworkInterest interest,
            Guid? manufacturerId,
            NamingInfo? namingInfo, 
            MarkingInfo? markingInfo)
        {
            this.Id = spanEquipmentId;

            if (spanEquipmentId == Guid.Empty)
                return Result.Fail(new PlaceSpanEquipmentInRouteNetworkError(PlaceSpanEquipmentInRouteNetworkErrorCodes.INVALID_SPAN_EQUIPMENT_ID_CANNOT_BE_EMPTY, "Span equipment id cannot be empty. A unique id must be provided by client."));

            if (spanEquipments.ContainsKey(spanEquipmentId))
                return Result.Fail(new PlaceSpanEquipmentInRouteNetworkError(PlaceSpanEquipmentInRouteNetworkErrorCodes.INVALID_SPAN_EQUIPMENT_ALREADY_EXISTS, $"A span equipment with id: {spanEquipmentId} already exists."));

            if (interest.Kind != RouteNetworkInterestKindEnum.WalkOfInterest)
                return Result.Fail(new PlaceSpanEquipmentInRouteNetworkError(PlaceSpanEquipmentInRouteNetworkErrorCodes.INVALID_INTEREST_KIND_MUST_BE_WALK_OF_INTEREST, "Interest kind must be WalkOfInterest."));

            if (!spanEquipmentSpecifications.ContainsKey(spanEquipmentSpecificationId))
                return Result.Fail(new PlaceSpanEquipmentInRouteNetworkError(PlaceSpanEquipmentInRouteNetworkErrorCodes.INVALID_SPAN_EQUIPMENT_SPECIFICATION_ID_NOT_FOUND, $"Cannot find span equipment specification with id: {spanEquipmentSpecificationId}"));

            var spanEquipment = CreateSpanEquipmentFromSpecification(
                spanEquipmentId: spanEquipmentId, 
                specification: spanEquipmentSpecifications[spanEquipmentSpecificationId], 
                walkOfInterestId: interest.Id, 
                nodesOfInterestIds: new Guid[] { interest.RouteNetworkElementRefs.First(), interest.RouteNetworkElementRefs.Last() }, 
                manufacturerId: manufacturerId, 
                namingInfo: namingInfo, 
                markingInfo: markingInfo
             );

            RaiseEvent(new SpanEquipmentPlacedInRouteNetwork(spanEquipment));

            return Result.Ok();
        }

        public Result AffixToNodeContainer(
            RouteNetworkInterest spanEquipmentInterest,
            Guid nodeContainerRouteNodeId,
            Guid nodeContainerId,
            Guid spanSegmentId,
            NodeContainerSideEnum nodeContainerIngoingSide)
        {
            if (!spanEquipmentInterest.RouteNetworkElementRefs.Contains(nodeContainerRouteNodeId))
            {
                return Result.Fail(new AffixSpanEquipmentToNodeContainerError(
                        AffixSpanEquipmentToNodeContainerErrorCodes.SPAN_EQUIPMENT_AND_NODE_CONTAINER_IS_NOT_COLOCATED,
                        $"The walk of span equipment with id: {this.Id} do not include the route network element with id: {nodeContainerRouteNodeId} where the node container with id: {nodeContainerId} is located.")
                    );
            }

            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (!_spanEquipment.TryGetSpanSegment(spanSegmentId, out var spanSegmentWithIndexInfo))
                throw new ApplicationException($"Cannot find any span segment with id: {spanSegmentId} inside span equipment with id: {this.Id}");

            var affix = new SpanEquipmentNodeContainerAffix(
                nodeContainerId: nodeContainerId,
                nodeContainerIngoingSide: nodeContainerIngoingSide
            );

            RaiseEvent(new SpanEquipmentAffixedToContainer(this.Id, affix));

            return Result.Ok();
        }

        public Result CutSpanSegments(Guid routeNodeId, Guid[] spanSegmentsToCut)
        {
            return Result.Fail("Not implemented");
        }

        private SpanEquipment CreateSpanEquipmentFromSpecification(Guid spanEquipmentId, SpanEquipmentSpecification specification, Guid walkOfInterestId, Guid[] nodesOfInterestIds, Guid? manufacturerId, NamingInfo? namingInfo, MarkingInfo? markingInfo)
        {
            List<SpanStructure> spanStructuresToInclude = new List<SpanStructure>();

            // Create root structure
            spanStructuresToInclude.Add(
                new SpanStructure(
                    id: Guid.NewGuid(), 
                    specificationId: specification.RootTemplate.SpanStructureSpecificationId, 
                    level: 1, 
                    parentPosition: 0, 
                    position: 1, 
                    spanSegments: new SpanSegment[] { new SpanSegment(Guid.NewGuid(), 0, 1) }
                )
            );

            // Add level 2 structures
            foreach (var template in specification.RootTemplate.GetAllSpanStructureTemplatesRecursive().Where(t => t.Level == 2))
            {
                spanStructuresToInclude.Add(
                    new SpanStructure(
                        id: Guid.NewGuid(),
                        specificationId: template.SpanStructureSpecificationId,
                        level: template.Level,
                        parentPosition: 1,
                        position: template.Position,
                        spanSegments: new SpanSegment[] { new SpanSegment(Guid.NewGuid(), 0, 1) }
                    )
                );
            }

            var spanEquipment = new SpanEquipment(spanEquipmentId, specification.Id, walkOfInterestId, nodesOfInterestIds, spanStructuresToInclude.ToArray())
            {
                ManufacturerId = manufacturerId,
                NamingInfo = namingInfo,
                MarkingInfo = markingInfo
            };

            return spanEquipment;
        }

        private void Apply(SpanEquipmentPlacedInRouteNetwork @event)
        {
            _spanEquipment = @event.Equipment;
        }

        private void Apply(SpanEquipmentAffixedToContainer @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }
    }
}
