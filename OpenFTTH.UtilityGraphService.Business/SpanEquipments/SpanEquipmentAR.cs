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
            Register<SpanSegmentsCut>(Apply);
            Register<SpanSegmentsConnectedToSimpleTerminals>(Apply);
        }

        #region Place Span Equipment
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

        private static SpanEquipment CreateSpanEquipmentFromSpecification(Guid spanEquipmentId, SpanEquipmentSpecification specification, Guid walkOfInterestId, Guid[] nodesOfInterestIds, Guid? manufacturerId, NamingInfo? namingInfo, MarkingInfo? markingInfo)
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
        #endregion

        #region Affix To Node Container
        public Result AffixToNodeContainer(
            LookupCollection<NodeContainer> nodeContainers,
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

            if (CheckIfAlreadyAffixedToNodeContainerInRouteNode(nodeContainers, nodeContainerRouteNodeId))
            {
                return Result.Fail(new AffixSpanEquipmentToNodeContainerError(
                        AffixSpanEquipmentToNodeContainerErrorCodes.SPAN_EQUIPMENT_ALREADY_AFFIXED_TO_NODE_CONTAINER,
                        $"The span equipment with id: {this.Id} is already affixed to the node container place the route node with id: {nodeContainerRouteNodeId}")
                    );
            }

            var affix = new SpanEquipmentNodeContainerAffix(
                routeNodeId: nodeContainerRouteNodeId,
                nodeContainerId: nodeContainerId,
                nodeContainerIngoingSide: nodeContainerIngoingSide
            );

            RaiseEvent(new SpanEquipmentAffixedToContainer(this.Id, affix));

            return Result.Ok();
        }

        private bool CheckIfAlreadyAffixedToNodeContainerInRouteNode(LookupCollection<NodeContainer> nodeContainers, Guid nodeContainerRouteNodeId)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (_spanEquipment.NodeContainerAffixes == null)
                return false;

            foreach (var affix in _spanEquipment.NodeContainerAffixes)
            {
                if (nodeContainers[affix.NodeContainerId].RouteNodeId == nodeContainerRouteNodeId)
                    return true;
            }

            return false;
        }

        private void Apply(SpanEquipmentAffixedToContainer @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }
        #endregion

        #region Cut Span Segments
        public Result CutSpanSegments(
            RouteNetworkInterest spanEquipmentWalkOfInterest, 
            Guid routeNodeId, 
            Guid[] spanSegmentsToCut)
        {
            if (routeNodeId == Guid.Empty)
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(CutSpanSegmentsAtRouteNodeErrorCodes.INVALID_ROUTE_NODE_ID_CANNOT_BE_EMPTY, "Route node id cannot be empty."));

            if (spanSegmentsToCut.Length == 0)
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(CutSpanSegmentsAtRouteNodeErrorCodes.INVALID_SPAN_SEGMENT_LIST_CANNOT_BE_EMPTY, "A list of span segments to cut must be provided."));

            if (!spanEquipmentWalkOfInterest.RouteNetworkElementRefs.Contains(routeNodeId))
            {
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                    CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_EQUIPMENT_AND_ROUTE_NODE_WHERE_TO_CUT_IS_NOT_COLOCATED,
                    $"The walk of span equipment with id: {this.Id} do not include the route network element with id: {routeNodeId} where to cut.")
                );
            }

            if (spanEquipmentWalkOfInterest.RouteNetworkElementRefs.First() == routeNodeId || spanEquipmentWalkOfInterest.RouteNetworkElementRefs.Last() == routeNodeId)
            {
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                    CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_EQUIPMENT_CANNOT_BE_CUT_AT_ENDS,
                    $"The route network node: {routeNodeId} is located at one of the ends of span equipment: {this.Id} This makes no sense. You cannot cut a span equipment at its ends.")
                );
            }

            // Chat that span equipment is affixed to container at node where the cuts are
            if (!IsAffixedToNodeContainer(routeNodeId))
            {
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                   CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_EQUIPMENT_NOT_AFFIXED_TO_NODE_CONTAINER,
                   $"Cutting span segments is only allowed if the span equipment: {this.Id} is affixed to a node container in route node: {routeNodeId}")
               );
            }

            // Check that cuts are valid
            var spanSegmentToCutHash = spanSegmentsToCut.ToHashSet();
            
            var validCutsResult = IsCutsValid(routeNodeId, spanSegmentToCutHash);
            
            if (validCutsResult.IsFailed)
                return validCutsResult;

            if (IsOuterSpanMissingToBeCut(routeNodeId, spanSegmentToCutHash))
            {
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                   CutSpanSegmentsAtRouteNodeErrorCodes.OUTER_SPAN_IS_NOT_CUT,
                   $"Cutting inner spans without cutting the outer span as well is not allowed. The outer span of span equipment with id: {this.Id} is currently not cut at route node with id: {routeNodeId}. Neither is the outer span specified to cut as part of the command.")
               );
            }

            // If we get to here, then everything should be in perfect order

            var @event = new SpanSegmentsCut(
                spanEquipmentId: this.Id,
                cutNodeOfInterestId: routeNodeId,
                cutNodeOfInterestIndex: GetCutNodeOfInterestIndex(routeNodeId, spanEquipmentWalkOfInterest),
                cuts: CreateCuts(spanSegmentsToCut)
            );

            RaiseEvent(@event);

            return Result.Ok();
        }
        private UInt16 GetCutNodeOfInterestIndex(Guid routeNodeId, RouteNetworkInterest walkOfInterest)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            // If node of interest list already contains route node, return that one
            if (_spanEquipment.NodesOfInterestIds.Contains(routeNodeId))
                return (UInt16)Array.IndexOf(_spanEquipment.NodesOfInterestIds, routeNodeId);

            // Find position where to insert new route node interest id
            HashSet<Guid> idsBeforeCut = new HashSet<Guid>();

            foreach (var routeNetworkElement in walkOfInterest.RouteNetworkElementRefs)
            {
                if (routeNetworkElement == routeNodeId)
                    break;

                idsBeforeCut.Add(routeNetworkElement);
            }
       
            for (UInt16 nodeOfInterestIndex = 0; nodeOfInterestIndex <= _spanEquipment.NodesOfInterestIds.Length; nodeOfInterestIndex++)
            {
                Guid nodeOfInterestId = _spanEquipment.NodesOfInterestIds[nodeOfInterestIndex];

                if (!idsBeforeCut.Contains(nodeOfInterestId))
                    return nodeOfInterestIndex;
            }

            throw new ApplicationException($"Error processing cut command. Cannot calculate the node of interest index where to cut. Span equipment: {this.Id} or command has an invalid state. Cut route node id: {routeNodeId} Span equipment walk of interest: {string.Join(",", walkOfInterest.RouteNetworkElementRefs)}");
        }

        private bool IsOuterSpanMissingToBeCut(Guid routeNodeId, HashSet<Guid> spanSegmentsToCut)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            // If outer span is already cut at node, no problem
            if (_spanEquipment.SpanStructures[0].SpanSegments.Any(s => 
                _spanEquipment.NodesOfInterestIds[s.FromNodeOfInterestIndex] == routeNodeId ||
                _spanEquipment.NodesOfInterestIds[s.ToNodeOfInterestIndex] == routeNodeId)) 
            {
                return false;
            }

            // If the spanSegmentsToCut include the outer span, no problem
            if (_spanEquipment.SpanStructures[0].SpanSegments.Any(s => spanSegmentsToCut.Contains(s.Id)))
            {
                return false;
            }

            // If we get to here, the outer span is not currently cut and is not being cut as part of the command either
            return true;
        }

        private bool IsAffixedToNodeContainer(Guid routeNodeId)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (_spanEquipment.NodeContainerAffixes == null)
                return false;

            foreach (var affix in _spanEquipment.NodeContainerAffixes)
            {
                if (affix.RouteNodeId == routeNodeId)
                    return true;
            }

            return false;
        }

        private Result IsCutsValid(Guid routeNodeId, HashSet<Guid> spanSegmentsToCut)
        {
            HashSet<Guid> spanSegmentsCutValidatedOk = new HashSet<Guid>();

            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            // Check that span segments are not already cut
            foreach (var structure in _spanEquipment.SpanStructures)
            {
                foreach (var segment in  structure.SpanSegments)
                {
                    if (spanSegmentsToCut.Contains(segment.Id))
                    {
                        // Check if span equipment already cut at node
                        if (_spanEquipment.NodesOfInterestIds[segment.FromNodeOfInterestIndex] == routeNodeId || _spanEquipment.NodesOfInterestIds[segment.ToNodeOfInterestIndex] == routeNodeId)
                        {
                            return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                                CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_ALREADY_CUT,
                                $"The span segment with id: {segment.Id} is already cut in route node with id: {routeNodeId}")
                            );
                        }
                        else
                        {
                            spanSegmentsCutValidatedOk.Add(segment.Id);
                        }
                    }
                }
            }


            // Check that we found all span segments
            var notFoundList = new List<Guid>();
            
            foreach (var segmentToCut in spanSegmentsToCut)
            {
                if (!spanSegmentsCutValidatedOk.Contains(segmentToCut))
                    notFoundList.Add(segmentToCut);
            }

            if (notFoundList.Count > 0)
            {
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                                CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND,
                                $"The span segment with ids: {string.Join(",", notFoundList)} was not found in span equipment with id: {this.Id} Notice that you cannot cut span segments belonging to multiple span equipments in the same command!")
                            );
            }

            return Result.Ok();
        }

        private SpanSegmentCutInfo[] CreateCuts(Guid[] spanSegmentsToCut)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            List<SpanSegmentCutInfo> cuts = new List<SpanSegmentCutInfo>();

            foreach (var spanSegmentId in spanSegmentsToCut)
            {
                if (!_spanEquipment.TryGetSpanSegment(spanSegmentId, out SpanSegmentWithIndexInfo spanSegmentWithIndexInfo))
                    throw new ApplicationException("Provided span equipment ids are not valid. The CreateCuts function should not be called before all cuts are proper validated!");

                cuts.Add(
                    new SpanSegmentCutInfo(
                       oldSpanSegmentId: spanSegmentId,
                       oldStructureIndex: spanSegmentWithIndexInfo.StructureIndex,
                       oldSegmentIndex: spanSegmentWithIndexInfo.SegmentIndex,
                       newSpanSegmentId1: Guid.NewGuid(),
                       newSpanSegmentId2: Guid.NewGuid()
                       )
                    );
            }

            return cuts.ToArray();
        }

        private void Apply(SpanSegmentsCut @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        #endregion


        #region Connect Span Segments To Simple Terminals
        public Result ConnectSpanSegmentsToSimpleTerminals(Guid routeNodeId,SpanSegmentToSimpleTerminalConnectInfo[] connects)
        {
            if (routeNodeId == Guid.Empty)
                return Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(ConnectSpanSegmentsAtRouteNodeErrorCodes.INVALID_ROUTE_NODE_ID_CANNOT_BE_EMPTY, "Route node id cannot be empty."));

            // Chat that span equipment is affixed to container at node where the connects should be created
            if (!IsAffixedToNodeContainer(routeNodeId))
            {
                return Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                   ConnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_EQUIPMENT_NOT_AFFIXED_TO_NODE_CONTAINER,
                   $"Connecting span segments is only allowed if the span equipment: {this.Id} is affixed to a node container in route node: {routeNodeId}")
               );
            }

            // Check that connects are valid
            var validConnectsResult = IsConnectsValid(routeNodeId, connects);

            if (validConnectsResult.IsFailed)
                return validConnectsResult;

            var @event = new SpanSegmentsConnectedToSimpleTerminals(
                spanEquipmentId: this.Id,
                connects: connects
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private Result IsConnectsValid(Guid routeNodeId, SpanSegmentToSimpleTerminalConnectInfo[] connects)
        {
            HashSet<Guid> spanSegmentsCutValidatedOk = new HashSet<Guid>();

            Dictionary<Guid, SpanSegmentToSimpleTerminalConnectInfo> connectsBySegmentId = connects.ToDictionary(c => c.SegmentId);

            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            // Check that span segments are not already cut
            foreach (var structure in _spanEquipment.SpanStructures)
            {
                foreach (var segment in structure.SpanSegments)
                {
                    if (connectsBySegmentId.TryGetValue(segment.Id, out var spanSegmentToSimpleTerminalConnectInfo))
                    {
                        // Check if span segment is connected to route node where to cut
                        if (_spanEquipment.NodesOfInterestIds[segment.FromNodeOfInterestIndex] != routeNodeId && _spanEquipment.NodesOfInterestIds[segment.ToNodeOfInterestIndex] != routeNodeId)
                        {
                            return Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                                ConnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_END_NOT_FOUND,
                                $"No ends of the span segment with id: {segment.Id} can be found in route node with id: {routeNodeId}")
                            );
                        }

                        // Check if already connected
                        if (spanSegmentToSimpleTerminalConnectInfo.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal && segment.ToTerminalId != Guid.Empty)
                        {
                            return Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                                   ConnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_ALREADY_CONNECTED,
                                   $"Span segment with id: {segment.Id} already connected to a terminal with id: {segment.ToTerminalId}")
                               );
                        }

                        if (spanSegmentToSimpleTerminalConnectInfo.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromTerminalToSpanSegment && segment.ToTerminalId != Guid.Empty)
                        {
                            return Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                                   ConnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_ALREADY_CONNECTED,
                                   $"Span segment with id: {segment.Id} already connected from a terminal with id: {segment.FromTerminalId}")
                               );
                        }


                        spanSegmentsCutValidatedOk.Add(segment.Id);
                    }
                }
            }


            // Check that we found all span segments
            var notFoundList = new List<Guid>();

            foreach (var segmentToConnect in connectsBySegmentId.Keys)
            {
                if (!spanSegmentsCutValidatedOk.Contains(segmentToConnect))
                    notFoundList.Add(segmentToConnect);
            }

            if (notFoundList.Count > 0)
            {
                return Result.Fail(new CutSpanSegmentsAtRouteNodeError(
                                CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND,
                                $"The span segment with ids: {string.Join(",", notFoundList)} was not found in span equipment with id: {this.Id} Notice that you cannot connect span segments belonging to multiple span equipments to terminal in the same command!")
                            );
            }

            return Result.Ok();
        }

        private void Apply(SpanSegmentsConnectedToSimpleTerminals @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }
        #endregion
    }
}
