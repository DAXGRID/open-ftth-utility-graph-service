using DAX.EventProcessing;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Changes;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class ConnectSpanSegmentsCommandHandler : ICommandHandler<ConnectSpanSegmentsAtRouteNode, Result>
    {
        // TODO: move into config
        private readonly string _topicName = "notification.utility-network";

        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly IExternalEventProducer _externalEventProducer;

        public ConnectSpanSegmentsCommandHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = externalEventProducer;
        }

        public Task<Result> HandleAsync(ConnectSpanSegmentsAtRouteNode command)
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            if (command.SpanSegmentsToConnect.Length == 0)
                return Task.FromResult(Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(ConnectSpanSegmentsAtRouteNodeErrorCodes.INVALID_SPAN_SEGMENT_LIST_CANNOT_BE_EMPTY, "A list of span segments to connect must be provided.")));

            // Because the client do not provide the span equipment ids, but span segment ids only,
            // we need lookup the span equipments via the the utility network graph
            Dictionary<Guid, SpanEquipmentWithConnectsHolder> spanEquipmentsToConnect = new Dictionary<Guid, SpanEquipmentWithConnectsHolder>();

            foreach (var spanSegmentToConnectId in command.SpanSegmentsToConnect)
            {
                if (!utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(spanSegmentToConnectId, out var spanSegmentGraphElement))
                    return Task.FromResult(Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(ConnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND, $"Cannot find any span segment in the utility graph with id: {spanSegmentToConnectId}")));

                var spanEquipment = spanSegmentGraphElement.SpanEquipment(utilityNetwork);
                var spanSegment = spanSegmentGraphElement.SpanSegment(utilityNetwork);

                if (!spanEquipmentsToConnect.ContainsKey(spanEquipment.Id))
                {
                    spanEquipment.TryGetSpanSegment(spanSegment.Id, out var spanSegmentWithIndexInfo);

                    spanEquipmentsToConnect.Add(
                       spanEquipment.Id,
                        new SpanEquipmentWithConnectsHolder()
                        {
                            SpanEquipment = spanEquipment,
                            Connects = new List<SpanSegmentConnectHolder> {
                                new SpanSegmentConnectHolder(
                                    new SpanSegmentToSimpleTerminalConnectInfo(
                                        segmentId: spanSegment.Id,
                                        terminalId: Guid.Empty
                                    )
                                )
                            }
                        }
                    );
                }
                else
                {
                    spanEquipment.TryGetSpanSegment(spanSegment.Id, out var spanSegmentWithIndexInfo);

                    spanEquipmentsToConnect[spanEquipment.Id].Connects.Add(
                        new SpanSegmentConnectHolder(
                            new SpanSegmentToSimpleTerminalConnectInfo(
                                segmentId: spanSegment.Id,
                                terminalId: Guid.Empty
                            )
                        )
                    );
                }
            }

            // Check that span segments from exactly two span equipment has been specified by the client
            if (spanEquipmentsToConnect.Count != 2)
            {
                return Task.FromResult(
                    Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                        ConnectSpanSegmentsAtRouteNodeErrorCodes.EXPECTED_SPAN_SEGMENTS_FROM_TWO_SPAN_EQUIPMENT,
                        $"Got span segments belonging to {spanEquipmentsToConnect.Count} This command can only handle connecting span segments between two span equipments.")
                    )
                );
            }

            var firstSpanEquipment = spanEquipmentsToConnect.Values.First();
            var secondSpanEquipment = spanEquipmentsToConnect.Values.Last();

            // Check that number of span segments from each span equipment is the same
            if (firstSpanEquipment.Connects.Count != secondSpanEquipment.Connects.Count)
            {
                return Task.FromResult(
                    Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                        ConnectSpanSegmentsAtRouteNodeErrorCodes.EXPECTED_SAME_NUMBER_OF_SPAN_SEGMENTS_BELONGING_TO_TWO_SPAN_EQUIPMENT,
                        $"Cannot connect the span segments specified because {firstSpanEquipment.Connects.Count} span segments are selected from span equipment: {firstSpanEquipment.SpanEquipment.Id} and {secondSpanEquipment.Connects.Count} span segments are selected from span equipment: {secondSpanEquipment.SpanEquipment.Id} The number of span segments selected in the two span equipments must the same!")
                    )
                );
            }

            // If more than 2 segments selected in each span equipment, check that they are alligned in terms of specifications
            if (firstSpanEquipment.Connects.Count > 1)
            {
                HashSet<Guid> firstSpanEquipmentStructureSpecIds = new HashSet<Guid>();
                HashSet<Guid> secondSpanEquipmentStructureSpecIds = new HashSet<Guid>();

                foreach (var firstEqSpanSegmentConnect in firstSpanEquipment.Connects)
                {
                    firstSpanEquipment.SpanEquipment.TryGetSpanSegment(firstEqSpanSegmentConnect.ConnectInfo.SegmentId, out var spanSegmentWithIndexInfo);
                    var structureSpecId = firstSpanEquipment.SpanEquipment.SpanStructures[spanSegmentWithIndexInfo.StructureIndex].SpecificationId;
                    firstEqSpanSegmentConnect.StructureSpecificationId = structureSpecId;
                    firstSpanEquipmentStructureSpecIds.Add(structureSpecId);
                }

                foreach (var secondEqSpanSegmentConnect in secondSpanEquipment.Connects)
                {
                    secondSpanEquipment.SpanEquipment.TryGetSpanSegment(secondEqSpanSegmentConnect.ConnectInfo.SegmentId, out var spanSegmentWithIndexInfo);
                    var structureSpecId = secondSpanEquipment.SpanEquipment.SpanStructures[spanSegmentWithIndexInfo.StructureIndex].SpecificationId;
                    secondEqSpanSegmentConnect.StructureSpecificationId = structureSpecId;
                    secondSpanEquipmentStructureSpecIds.Add(structureSpecId);
                }

                foreach (var firstStructureId in firstSpanEquipmentStructureSpecIds)
                {
                    if (!secondSpanEquipmentStructureSpecIds.Contains(firstStructureId))
                    {
                        return Task.FromResult(
                            Result.Fail(new ConnectSpanSegmentsAtRouteNodeError(
                                ConnectSpanSegmentsAtRouteNodeErrorCodes.EXPECTED_SAME_SPECIFICATIONS_OF_SPAN_SEGMENTS_BELONGING_TO_TWO_SPAN_EQUIPMENT,
                                $"Cannot connect the span segments specified because specifications on the span structures don't allign. Make sure that you connect span segments beloning to structure with the same specs - i.e. a red and blue Ø10 from one span equipment to a red and blue Ø10 in the other span equipment.")
                            )
                        );

                    }
                }

                // Order connects by spec id
                firstSpanEquipment.Connects = firstSpanEquipment.Connects.OrderBy(s => s.StructureSpecificationId).ToList();
                secondSpanEquipment.Connects = secondSpanEquipment.Connects.OrderBy(s => s.StructureSpecificationId).ToList();
            }

            // Create junction/terminal ids used to connect span segments
            for (int i = 0; i < firstSpanEquipment.Connects.Count; i++)
            {
                var junctionId = Guid.NewGuid();

                firstSpanEquipment.Connects[i].ConnectInfo.TerminalId = junctionId;
                firstSpanEquipment.Connects[i].ConnectInfo.ConnectionDirection = SpanSegmentToTerminalConnectionDirection.FromSpanSegmentToTerminal;

                secondSpanEquipment.Connects[i].ConnectInfo.TerminalId = junctionId;
                secondSpanEquipment.Connects[i].ConnectInfo.ConnectionDirection = SpanSegmentToTerminalConnectionDirection.FromTerminalToSpanSegment;
            }

            // Connect the first span equipment to terminals
            var firstSpanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(firstSpanEquipment.SpanEquipment.Id);

            var firstSpanEquipmentConnectResult = firstSpanEquipmentAR.ConnectSpanSegmentsToSimpleTerminals(
                routeNodeId: command.RouteNodeId,
                connects: firstSpanEquipment.Connects.Select(c => c.ConnectInfo).ToArray()
            );

            if (!firstSpanEquipmentConnectResult.IsSuccess)
                return Task.FromResult(firstSpanEquipmentConnectResult);

            // Connect the second span equipment to terminals
            var secondSpanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(secondSpanEquipment.SpanEquipment.Id);

            var secondSpanEquipmentConnectResult = secondSpanEquipmentAR.ConnectSpanSegmentsToSimpleTerminals(
                routeNodeId: command.RouteNodeId,
                connects: secondSpanEquipment.Connects.Select(c => c.ConnectInfo).ToArray()
            );

            if (!secondSpanEquipmentConnectResult.IsSuccess)
                return Task.FromResult(firstSpanEquipmentConnectResult);

            _eventStore.Aggregates.Store(firstSpanEquipmentAR);
            _eventStore.Aggregates.Store(secondSpanEquipmentAR);

            NotifyExternalServicesAboutChange(firstSpanEquipment.SpanEquipment.Id, secondSpanEquipment.SpanEquipment.Id, command.RouteNodeId);

            return Task.FromResult(Result.Ok());
        }

        private async void NotifyExternalServicesAboutChange(Guid firstSpanEquipmentId, Guid secondSpanEquipmentId, Guid routeNodeId)
        {
            List<IdChangeSet> idChangeSets = new List<IdChangeSet>
            {
                new IdChangeSet("SpanEquipment", ChangeTypeEnum.Modification, new Guid[] { firstSpanEquipmentId, secondSpanEquipmentId })
            };

            var updatedEvent =
                new RouteNetworkElementContainedEquipmentUpdated(
                    eventType: typeof(RouteNetworkElementContainedEquipmentUpdated).Name,
                    eventId: Guid.NewGuid(),
                    eventTimestamp: DateTime.UtcNow,
                    applicationName: "UtilityNetworkService",
                    applicationInfo: null,
                    category: "EquipmentConnectivityModification.Connect",
                    idChangeSets: idChangeSets.ToArray(),
                    affectedRouteNetworkElementIds: new Guid[] { routeNodeId }
                );

            await _externalEventProducer.Produce(_topicName, updatedEvent);
        }

        private class SpanEquipmentWithConnectsHolder
        {
            public SpanEquipment SpanEquipment { get; set; }
            public List<SpanSegmentConnectHolder> Connects { get; set; }
        }

        private class SpanSegmentConnectHolder
        {
            public SpanSegmentToSimpleTerminalConnectInfo ConnectInfo { get; }
            public Guid StructureSpecificationId { get; set; }

            public SpanSegmentConnectHolder(SpanSegmentToSimpleTerminalConnectInfo connectInfo)
            {
                ConnectInfo = connectInfo;
            }
        }
    }
}

  