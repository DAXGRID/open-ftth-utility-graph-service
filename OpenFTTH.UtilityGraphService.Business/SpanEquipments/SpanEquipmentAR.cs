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
        private bool _removed;

        public SpanEquipmentAR()
        {
            Register<SpanEquipmentPlacedInRouteNetwork>(Apply);
            Register<SpanEquipmentAffixedToContainer>(Apply);
            Register<SpanSegmentsCut>(Apply);
            Register<SpanSegmentsConnectedToSimpleTerminals>(Apply);
            Register<SpanSegmentDisconnectedFromTerminal>(Apply);
            Register<SpanEquipmentDetachedFromContainer>(Apply);
            Register<AdditionalStructuresAddedToSpanEquipment>(Apply);
            Register<SpanStructureRemoved>(Apply);
            Register<SpanEquipmentRemoved>(Apply);
            Register<SpanEquipmentMoved>(Apply);
            Register<SpanEquipmentMerged>(Apply);
            Register<MarkingInfoUpdated>(Apply);
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

            if (IsAlreadyAffixedToNodeContainerInRouteNode(nodeContainers, nodeContainerRouteNodeId))
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

        private bool IsAlreadyAffixedToNodeContainerInRouteNode(LookupCollection<NodeContainer> nodeContainers, Guid nodeContainerRouteNodeId)
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

        #region Detach From Node Container
        public Result DetachFromNodeContainer(NodeContainer nodeContainer)
        {
            if (!IsAffixedToNodeContainer(nodeContainer.Id))
            {
                return Result.Fail(new DetachSpanEquipmentFromNodeContainerError(
                        DetachSpanEquipmentFromNodeContainerErrorCodes.SPAN_EQUIPMENT_IS_NOT_AFFIXED_TO_NODE_CONTAINER,
                        $"The span equipment with id: {this.Id} is not affixed to the node container: {nodeContainer.Id}")
                    );
            }

            var canBeDetachedResult = CanBeDetached(nodeContainer);

            if (canBeDetachedResult.IsFailed)
                return canBeDetachedResult;

            RaiseEvent(new SpanEquipmentDetachedFromContainer(this.Id, nodeContainer.Id));

            return Result.Ok();
        }

        private void Apply(SpanEquipmentDetachedFromContainer @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        private Result CanBeDetached(NodeContainer nodeContainer)
        {
            HashSet<Guid> spanSegmentsCutValidatedOk = new HashSet<Guid>();

            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            // Check all span segments that are connected to/from the route node of the node container
            foreach (var structure in _spanEquipment.SpanStructures)
            {
                foreach (var segment in structure.SpanSegments)
                {
                    if (_spanEquipment.NodesOfInterestIds[segment.FromNodeOfInterestIndex] == nodeContainer.RouteNodeId)
                    {
                        if (segment.FromTerminalId != Guid.Empty)
                        {
                            return Result.Fail(new DetachSpanEquipmentFromNodeContainerError(
                                DetachSpanEquipmentFromNodeContainerErrorCodes.SPAN_SEGMENT_IS_CONNECTED_INSIDE_NODE_CONTAINER,
                                $"The span segment with id: {segment.Id} is connected inside the node container with id: {nodeContainer.Id} Cannot detach a span equipment from a node container if any connected span segments.")
                            );
                        }

                        // If span equipment is passing through node container, the span segment must be cut, which is not allowed either
                        if (nodeContainer.RouteNodeId != _spanEquipment.NodesOfInterestIds.First() && nodeContainer.RouteNodeId != _spanEquipment.NodesOfInterestIds.Last())
                        {
                            return Result.Fail(new DetachSpanEquipmentFromNodeContainerError(
                                DetachSpanEquipmentFromNodeContainerErrorCodes.SPAN_SEGMENT_IS_CUT_INSIDE_NODE_CONTAINER,
                                $"The span segment with id: {segment.Id} is cut inside the node container with id: {nodeContainer.Id} Cannot detach a span equipment from a node container if any span segments are cut.")
                            );
                        }
                    }

                    if (_spanEquipment.NodesOfInterestIds[segment.ToNodeOfInterestIndex] == nodeContainer.RouteNodeId)
                    {
                        if (segment.ToTerminalId != Guid.Empty)
                        {
                            return Result.Fail(new DetachSpanEquipmentFromNodeContainerError(
                                DetachSpanEquipmentFromNodeContainerErrorCodes.SPAN_SEGMENT_IS_CONNECTED_INSIDE_NODE_CONTAINER,
                                $"The span segment with id: {segment.Id} is connected inside the node container with id: {nodeContainer.Id} Cannot detach a span equipment from a node container if any connected span segments.")
                            );
                        }

                        // If span equipment is passing through node container, the span segment must be cut, which is not allowed either
                        if (nodeContainer.RouteNodeId != _spanEquipment.NodesOfInterestIds.First() && nodeContainer.RouteNodeId != _spanEquipment.NodesOfInterestIds.Last())
                        {
                            return Result.Fail(new DetachSpanEquipmentFromNodeContainerError(
                                DetachSpanEquipmentFromNodeContainerErrorCodes.SPAN_SEGMENT_IS_CUT_INSIDE_NODE_CONTAINER,
                                $"The span segment with id: {segment.Id} is cut inside the node container with id: {nodeContainer.Id} Cannot detach a span equipment from a node container if any span segments are cut.")
                            );
                        }
                    }

                }
            }

            return Result.Ok();
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

        private bool IsAffixedToNodeContainer(Guid routeNodeOrContainerId)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (_spanEquipment.NodeContainerAffixes == null)
                return false;

            foreach (var affix in _spanEquipment.NodeContainerAffixes)
            {
                if (affix.RouteNodeId == routeNodeOrContainerId || affix.NodeContainerId == routeNodeOrContainerId)
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
                        if (_spanEquipment.NodesOfInterestIds[segment.FromNodeOfInterestIndex] == routeNodeId)
                        {
                            spanSegmentToSimpleTerminalConnectInfo.ConnectionDirection = SpanSegmentToTerminalConnectionDirection.FromTerminalToSpanSegment;
                        }
                        else if (_spanEquipment.NodesOfInterestIds[segment.ToNodeOfInterestIndex] == routeNodeId)
                        {
                            spanSegmentToSimpleTerminalConnectInfo.ConnectionDirection = SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal;
                        }
                        else 
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

                        if (spanSegmentToSimpleTerminalConnectInfo.ConnectionDirection == SpanSegmentToTerminalConnectionDirection.FromTerminalToSpanSegment && segment.FromTerminalId != Guid.Empty)
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

        #region Disconnect Span Segment From Terminal
        public Result DisconnectSegmentFromTerminal(Guid spanSegmentId, Guid terminalId)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (!_spanEquipment.TryGetSpanSegment(spanSegmentId, out var spanSegmentWithIndexInfo))
            {
                return Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND, $"Cannot find any span segment with id: {spanSegmentId} in span equipment with id: {this.Id}"));
            }

            if (!IsConnected(spanSegmentWithIndexInfo.SpanSegment, terminalId))
            {
                return Result.Fail(new DisconnectSpanSegmentsAtRouteNodeError(DisconnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_CONNECTED_TO_TERMINAL, $"Span segment with id: {spanSegmentId} not connected to termian: {terminalId} in span equipment with id: {this.Id}"));
            }

            var @event = new SpanSegmentDisconnectedFromTerminal(
               spanEquipmentId: this.Id,
               spanSegmentId: spanSegmentId,
               terminalId: terminalId
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(SpanSegmentDisconnectedFromTerminal @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        private bool IsConnected(SpanSegment spanSegment, Guid terminalId)
        {
            if (spanSegment.FromTerminalId == terminalId || spanSegment.ToTerminalId == terminalId)
                return true;
            else
                return false;
        }

        #endregion

        #region Add Additional Structures
        public Result AddAdditionalStructures(SpanEquipmentSpecification specification, LookupCollection<SpanStructureSpecification> structureSpecifications, Guid[] structureSpecificationIdsToAdd)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (structureSpecifications.Count == 0)
            {
                return Result.Fail(new PlaceAdditionalStructuresInSpanEquipmentError(
                   PlaceAdditionalStructuresInSpanEquipmentErrorCodes.INVALID_STRUCTURE_SPECIFICAION_ID_LIST_CANNOT_BE_EMPTY,
                   $"Must provide at least 1 structure specification id")
               );
            }

            // Check that all specifications are there
            foreach (var structureSpecificationId in structureSpecificationIdsToAdd)
            {
                if (!structureSpecifications.ContainsKey(structureSpecificationId))
                {
                    return Result.Fail(new PlaceAdditionalStructuresInSpanEquipmentError(
                        PlaceAdditionalStructuresInSpanEquipmentErrorCodes.INVALID_STRUCTURE_SPECIFICATION_ID_NOT_FOUND,
                        $"Cannot find structure specification with id: {structureSpecificationId}")
                    );
                }
            }

            if (specification.IsFixed)
            {
                return Result.Fail(new PlaceAdditionalStructuresInSpanEquipmentError(
                    PlaceAdditionalStructuresInSpanEquipmentErrorCodes.CANNOT_ADD_SPAN_STRUCTURES_TO_FIXED_SPAN_EQUIPMENT,
                    $"Cannot add span structures to a span equipment with fixed structure - i.e. to a span equipment with pre-installed inner conduits")
                );
            }

            List<SpanStructure> spanStructuresToInclude = new List<SpanStructure>();

            var innerStructureStartPosition = 1;

            if (_spanEquipment.SpanStructures.Any(s => s.Level == 2))
                innerStructureStartPosition = _spanEquipment.SpanStructures.Where(s => s.Level == 2).Max(s => s.Position) + 1;
                       
            // Add structures to level 2
            foreach (var structureSpecificationId in structureSpecificationIdsToAdd)
            {
                spanStructuresToInclude.Add(
                    new SpanStructure(
                        id: Guid.NewGuid(),
                        specificationId: structureSpecificationId,
                        level: 2,
                        parentPosition: 1,
                        position: (ushort)innerStructureStartPosition,
                        spanSegments: new SpanSegment[] { new SpanSegment(Guid.NewGuid(), 0, (ushort)(_spanEquipment.NodesOfInterestIds.Length - 1)) }
                    )
                );

                innerStructureStartPosition++;
            }

            var @event = new AdditionalStructuresAddedToSpanEquipment(
               spanEquipmentId: this.Id,
               spanStructuresToAdd: spanStructuresToInclude.ToArray()
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(AdditionalStructuresAddedToSpanEquipment @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        #endregion

        #region Remove Span Structure

        public Result RemoveSpanStructure(SpanEquipmentSpecification specification, ushort structureIndex)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (specification.IsFixed)
            {
                return Result.Fail(new RemoveSpanStructureFromSpanEquipmentError(
                    RemoveSpanStructureFromSpanEquipmentErrorCodes.CANNOT_REMOVE_SPAN_STRUCTURES_FROM_FIXED_SPAN_EQUIPMENT,
                    $"Cannot remove span structures from a span equipment with fixed structure - i.e. from a span equipment with pre-installed inner conduits")
                ); 
            }

            if (structureIndex == 0)
                throw new ApplicationException("You can't use the RemoveSpanStructure method to remove the outer span structure. In such case you need to remove the entire span equipment.");

            if (structureIndex >= _spanEquipment.SpanStructures.Length || structureIndex < 0)
                throw new ApplicationException("Structure index out of bounds.");

            if (IsAnySpanSegmentsInStructureConnected(structureIndex))
            {
                return Result.Fail(new RemoveSpanStructureFromSpanEquipmentError(
                    RemoveSpanStructureFromSpanEquipmentErrorCodes.CANNOT_REMOVE_SPAN_STRUCTURE_WITH_CONNECTED_SEGMENTS_FROM_SPAN_EQUIPMENT,
                    $"Cannot remove span structures from a span equipment if some of its segments are connected")
                );
            }


            var spanStructureToRemove = _spanEquipment.SpanStructures[structureIndex];

            var @event = new SpanStructureRemoved(
               spanEquipmentId: this.Id,
               spanStructureId: spanStructureToRemove.Id
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private bool IsAnySpanSegmentsInStructureConnected(ushort structureIndex)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            var spanStructure = _spanEquipment.SpanStructures[structureIndex];

            foreach (var spanSegment in spanStructure.SpanSegments)
            {
                if (spanSegment.FromTerminalId != Guid.Empty || spanSegment.ToTerminalId != Guid.Empty)
                    return true;
            }

            return false;
        }

        private void Apply(SpanStructureRemoved @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        #endregion

        #region Remove
        public Result Remove()
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (IsAnySpanSegmentsConnected())
            {
                return Result.Fail(new RemoveSpanStructureFromSpanEquipmentError(
                    RemoveSpanStructureFromSpanEquipmentErrorCodes.CANNOT_REMOVE_SPAN_EQUIPMENT_WITH_CONNECTED_SEGMENTS,
                    $"Cannot remove a span equipment if some of its segments are connected")
                );
            }

            var @event = new SpanEquipmentRemoved(
               spanEquipmentId: this.Id
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(SpanEquipmentRemoved @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _removed = true;
        }

        private bool IsAnySpanSegmentsConnected()
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            foreach (var spanStructure in _spanEquipment.SpanStructures)
            {
                foreach (var spanSegment in spanStructure.SpanSegments)
                {
                    if (spanSegment.FromTerminalId != Guid.Empty || spanSegment.ToTerminalId != Guid.Empty)
                        return true;
                }
            }

            return false;
        }

        #endregion

        #region Move
        public Result Move(ValidatedRouteNetworkWalk newWalk, ValidatedRouteNetworkWalk existingWalk)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            // If both ends are moved, there cannot be any cuts/breakouts.
            if (newWalk.FromNodeId != existingWalk.FromNodeId && newWalk.ToNodeId != existingWalk.ToNodeId && _spanEquipment.NodesOfInterestIds.Length > 2)
            {
                return Result.Fail(new MoveSpanEquipmentError(
                   MoveSpanEquipmentErrorCodes.CANNOT_MOVE_BOTH_ENDS_AT_THE_SAME_TIME_IF_SPAN_SEGMENT_HAS_CUTS,
                   $"Cannot move both ends of the walk at the same time, when the span equipment has cuts/breakouts. This because we then have no idea if the walk has been reversed, which might lead to inconsistency in the segment connectivity direction inside the span equipment. The user has to moving one end at the time in this situation.")
               );
            }

            // If from end is moved
            if (newWalk.FromNodeId != existingWalk.FromNodeId)
            {
                // There cannot be any connection in the node moved away from
                if (IsAnySpanSegmentsConnectedInNode(existingWalk.FromNodeId))
                {
                    return Result.Fail(new MoveSpanEquipmentError(
                       MoveSpanEquipmentErrorCodes.CANNOT_MOVE_FROM_END_BECAUSE_SEGMENTS_ARE_CONNECTED_THERE,
                       $"Cannot move from end from: {existingWalk.FromNodeId} to: {newWalk.FromNodeId} because segments exists that has connections to other equipment in that node.")
                   );
                }

                // There cannot be any cuts in the node moved to
                if (IntermediateCutNodeIds.Contains(newWalk.FromNodeId))
                {
                    return Result.Fail(new MoveSpanEquipmentError(
                       MoveSpanEquipmentErrorCodes.CANNOT_MOVE_FROM_END_TO_NODE_WHERE_SEGMENTS_ARE_CUT,
                       $"Cannot move from end from: {existingWalk.FromNodeId} to: {newWalk.FromNodeId} because segments exists that are cut in that node.")
                   );
                }
            }

            // If to end is moved
            if (newWalk.ToNodeId != existingWalk.ToNodeId)
            {
                // There cannot be any connection in the node moved away from
                if (IsAnySpanSegmentsConnectedInNode(existingWalk.ToNodeId))
                {
                    return Result.Fail(new MoveSpanEquipmentError(
                       MoveSpanEquipmentErrorCodes.CANNOT_MOVE_TO_END_BECAUSE_SEGMENTS_ARE_CONNECTED_THERE,
                       $"Cannot move to end from: {existingWalk.ToNodeId} to: {newWalk.ToNodeId} because segments exists that has connections to other equipment in that node.")
                   );
                }

                // There cannot be any cuts in the node moved to
                if (IntermediateCutNodeIds.Contains(newWalk.ToNodeId))
                {
                    return Result.Fail(new MoveSpanEquipmentError(
                       MoveSpanEquipmentErrorCodes.CANNOT_MOVE_TO_END_TO_NODE_WHERE_SEGMENTS_ARE_CUT,
                       $"Cannot move to end from: {existingWalk.ToNodeId} to: {newWalk.ToNodeId} because segments exists that are cut in that node.")
                   );
                }
            }

            // Check that span equipment is not moved away from nodes where it is cut
            foreach (var nodeOfInterestId in IntermediateCutNodeIds)
            {
                if (!newWalk.RouteNetworkElementRefs.Contains(nodeOfInterestId))
                {
                    return Result.Fail(new MoveSpanEquipmentError(
                        MoveSpanEquipmentErrorCodes.CANNOT_MOVE_NODE_BECAUSE_SEGMENTS_ARE_CUT_THERE,
                        $"Cannot move span equipment away from node: {nodeOfInterestId} because segments are cut in this node.")
                    );
                }
            }

            // Check that span equipment is not moved away from nodes where it is affixed to a container
            if (_spanEquipment.NodeContainerAffixes != null)
            {
                foreach (var nodeContainerAffix in _spanEquipment.NodeContainerAffixes)
                {
                    if (!newWalk.RouteNetworkElementRefs.Contains(nodeContainerAffix.RouteNodeId))
                    {
                        return Result.Fail(new MoveSpanEquipmentError(
                            MoveSpanEquipmentErrorCodes.CANNOT_MOVE_NODE_BECAUSE_SPAN_EQUIPMENT_IS_AFFIXED_TO_CONTAINER,
                            $"Cannot move span equipment away from node: {nodeContainerAffix.RouteNodeId} because span equipment is affixed to a container in this node.")
                        );
                    }
                }
            }


            var @event = new SpanEquipmentMoved(
              spanEquipmentId: this.Id,
              nodesOfInterestIds: CreateNewNodesOfInterestIdList(newWalk)
            );

            RaiseEvent(@event);

            return Result.Ok();

        }

        private Guid[] IntermediateCutNodeIds
        {
            get
            {
                if (_spanEquipment == null)
                    throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

                List<Guid> result = new List<Guid>();

                for (int i = 1; i < (_spanEquipment.NodesOfInterestIds.Length - 1); i++)
                {
                    result.Add(_spanEquipment.NodesOfInterestIds[i]);
                }

                return result.ToArray();
            }
        }

        private Guid[] CreateNewNodesOfInterestIdList(ValidatedRouteNetworkWalk newWalk)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            Guid[] result = new Guid[_spanEquipment.NodesOfInterestIds.Length];

            result[0] = newWalk.FromNodeId;
            result[^1] = newWalk.ToNodeId;

            return result;
        }

        private bool IsAnySpanSegmentsConnectedInNode(Guid nodeId)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            var nodeIndex = Array.IndexOf(_spanEquipment.NodesOfInterestIds, nodeId);

            if (nodeIndex < 0)
                throw new ApplicationException($"Cannot find node with id: {nodeId} in span equipment nodes of interest");

            foreach (var spanStructure in _spanEquipment.SpanStructures)
            {
                foreach (var spanSegment in spanStructure.SpanSegments)
                {
                    if (spanSegment.FromTerminalId != Guid.Empty && spanSegment.FromNodeOfInterestIndex == nodeIndex)
                        return true;

                    if (spanSegment.ToTerminalId != Guid.Empty && spanSegment.ToNodeOfInterestIndex == nodeIndex)
                        return true;
                }
            }

            return false;
        }

        private void Apply(SpanEquipmentMoved @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        #endregion

        #region Merge
        public Result Merge(Guid routeNodeId, SpanEquipment spanEquipmentToMergeWith)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if (_spanEquipment.SpecificationId != spanEquipmentToMergeWith.SpecificationId)
            {
                return Result.Fail(new MergeSpanEquipmentError(
                        MergeSpanEquipmentErrorCodes.CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_OF_SPECIFICATION_MISMATCH,
                        $"Cannot merge span equipment: {_spanEquipment.Id} with span equipment: {spanEquipmentToMergeWith.Id} because their specification is not the same.")
                    );
            }

            // This span equipment must end in the route node specificed
            if (_spanEquipment.NodesOfInterestIds.First() != routeNodeId && _spanEquipment.NodesOfInterestIds.Last() != routeNodeId)
            {
                return Result.Fail(new MergeSpanEquipmentError(
                       MergeSpanEquipmentErrorCodes.CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_ENDS_ARE_NOT_COLOCATED_IN_ROUTE_NODE,
                       $"Cannot merge span equipment: {_spanEquipment.Id} with span equipment: {spanEquipmentToMergeWith.Id} because no ends of the former is ending in route node: {routeNodeId}")
                   );
            }

            // The other span equipment must end in the route node specificed as well
            if (spanEquipmentToMergeWith.NodesOfInterestIds.First() != routeNodeId && spanEquipmentToMergeWith.NodesOfInterestIds.Last() != routeNodeId)
            {
                return Result.Fail(new MergeSpanEquipmentError(
                       MergeSpanEquipmentErrorCodes.CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_ENDS_ARE_NOT_COLOCATED_IN_ROUTE_NODE,
                       $"Cannot merge span equipment: {_spanEquipment.Id} with span equipment: {spanEquipmentToMergeWith.Id} because no ends of the later is ending in route node: {routeNodeId}")
                   );
            }

            // The span equipment we want to merge with this one cannot contain any connections (because it's deleted)
            if (spanEquipmentToMergeWith.SpanStructures.Any(st => st.SpanSegments.Any(se => se.FromTerminalId != Guid.Empty || se.ToTerminalId != Guid.Empty)))
            {
                return Result.Fail(new MergeSpanEquipmentError(
                        MergeSpanEquipmentErrorCodes.CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_OF_CONNECTIVITY,
                        $"Cannot merge span equipment: {_spanEquipment.Id} with span equipment: {spanEquipmentToMergeWith.Id} because the later has connectivity to other span segments.")
                    );
            }

            // This span equipment cannot be affixed to node container in merge node
            if (_spanEquipment.NodeContainerAffixes != null && _spanEquipment.NodeContainerAffixes.Any(affix => affix.RouteNodeId == routeNodeId))
            {
                return Result.Fail(new MergeSpanEquipmentError(
                        MergeSpanEquipmentErrorCodes.CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_END_IS_AFFIXED_TO_NODE_CONTAINER,
                        $"Cannot merge span equipment: {_spanEquipment.Id} with span equipment: {spanEquipmentToMergeWith.Id} because the former is affixed to a node container in route node: {routeNodeId}")
                    );
            }

            // The span equipment we want to merge with cannot be affixed to node container in merge node
            if (spanEquipmentToMergeWith.NodeContainerAffixes != null && spanEquipmentToMergeWith.NodeContainerAffixes.Any(affix => affix.RouteNodeId == routeNodeId))
            {
                return Result.Fail(new MergeSpanEquipmentError(
                        MergeSpanEquipmentErrorCodes.CANNOT_MERGE_SPAN_EQUIPMENT_BECAUSE_END_IS_AFFIXED_TO_NODE_CONTAINER,
                        $"Cannot merge span equipment: {_spanEquipment.Id} with span equipment: {spanEquipmentToMergeWith.Id} because the later is affixed to a node container in route node: {routeNodeId}")
                    );
            }


            // Create a new node of interest id array that include the new end from the other span equipment resulting from the merge
            Guid[] updatedNodeOfInterestIds = new Guid[_spanEquipment.NodesOfInterestIds.Length];
            _spanEquipment.NodesOfInterestIds.CopyTo(updatedNodeOfInterestIds, 0);

            if (_spanEquipment.NodesOfInterestIds.First() == spanEquipmentToMergeWith.NodesOfInterestIds.First())
                updatedNodeOfInterestIds[0] = spanEquipmentToMergeWith.NodesOfInterestIds.Last();
            else if (_spanEquipment.NodesOfInterestIds.Last() == spanEquipmentToMergeWith.NodesOfInterestIds.First())
                updatedNodeOfInterestIds[^1] = spanEquipmentToMergeWith.NodesOfInterestIds.Last();
            else if (_spanEquipment.NodesOfInterestIds.First() == spanEquipmentToMergeWith.NodesOfInterestIds.Last())
                updatedNodeOfInterestIds[0] = spanEquipmentToMergeWith.NodesOfInterestIds.First();
            else if (_spanEquipment.NodesOfInterestIds.Last() == spanEquipmentToMergeWith.NodesOfInterestIds.Last())
                updatedNodeOfInterestIds[^1] = spanEquipmentToMergeWith.NodesOfInterestIds.First();
            else
                throw new ApplicationException("There an unexpected error in shared node validation and/or identification");


            var @event = new SpanEquipmentMerged(
              spanEquipmentId: this.Id,
              nodesOfInterestIds: updatedNodeOfInterestIds
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(SpanEquipmentMerged @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        #endregion

        #region Update Marking Info
        public Result UpdateMarkingInfo(MarkingInfo? markingInfo)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            if ((_spanEquipment.MarkingInfo == null && markingInfo == null) || (_spanEquipment.MarkingInfo != null && _spanEquipment.MarkingInfo.Equals(markingInfo)))
            {
                return Result.Fail(new UpdateSpanEquipmentPropertiesError(
                       UpdateSpanEquipmentPropertiesErrorCodes.NO_CHANGE_TO_MARKING_INFO,
                       $"Will not update marking info, because the provided value is equal the existing value.")
                   );
            }

            var @event = new MarkingInfoUpdated(
              spanEquipmentId: this.Id,
              markingInfo: markingInfo
            );

            RaiseEvent(@event);

            return Result.Ok();
        }

        private void Apply(MarkingInfoUpdated @event)
        {
            if (_spanEquipment == null)
                throw new ApplicationException($"Invalid internal state. Span equipment property cannot be null. Seems that span equipment has never been placed. Please check command handler logic.");

            _spanEquipment = SpanEquipmentProjectionFunctions.Apply(_spanEquipment, @event);
        }

        #endregion
    }
}
