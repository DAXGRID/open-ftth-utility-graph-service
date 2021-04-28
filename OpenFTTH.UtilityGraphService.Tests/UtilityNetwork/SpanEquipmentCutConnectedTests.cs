﻿using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.UtilityNetwork;
using OpenFTTH.EventSourcing;
using OpenFTTH.TestData;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(1100)]
    public class SpanEquipmentCutConnectedTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public SpanEquipmentCutConnectedTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact,Order(1)]
        public async void TestConnectTwo5x10inCO1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutConnectFromSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_CO_1_to_HH_1;
            var sutConnectToSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectFromSpanEquipment, out var sutFromSpanEquipment);
            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectToSpanEquipment, out var sutToSpanEquipment);

            var connectCmd = new ConnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.HH_1,
                spanSegmentsToConnect: new Guid[] {
                    sutFromSpanEquipment.SpanStructures[1].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[1].SpanSegments[0].Id,
                    sutFromSpanEquipment.SpanStructures[2].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[2].SpanSegments[0].Id,
                    sutFromSpanEquipment.SpanStructures[3].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[3].SpanSegments[0].Id,
                    sutFromSpanEquipment.SpanStructures[4].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[4].SpanSegments[0].Id,
                    sutFromSpanEquipment.SpanStructures[5].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[5].SpanSegments[0].Id,
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            // Assert
            connectResult.IsSuccess.Should().BeTrue();
        }

        [Fact, Order(2)]
        public async void CutConduitInCC1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentBeforeCut);

            // Cut inner conduit 5
            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToCut: new Guid[] {
                    spanEquipmentBeforeCut.SpanStructures[5].SpanSegments[0].Id
                }
            );

            var cutResult = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentAfterCut);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { sutSpanEquipment })
            );

            equipmentQueryResult.IsSuccess.Should().BeTrue();

            // Assert
            cutResult.IsSuccess.Should().BeTrue();

            // Check that inner conduit 1 is connected correctly in graph
            var innerConduit1Segment1Id = spanEquipmentAfterCut.SpanStructures[1].SpanSegments[0].Id;

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit1Segment1Id, out var conduit1segment1UtilityGraphElement);

            conduit1segment1UtilityGraphElement.Should().NotBeNull();

            var traceResult = utilityNetwork.Graph.TraceSegment(innerConduit1Segment1Id);

            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CO_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.SP_1);


            // Check that inner conduit 5 segment 1 is connected correctly in graph
            var innerConduit5Segment1Id = spanEquipmentAfterCut.SpanStructures[5].SpanSegments[0].Id;

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit5Segment1Id, out var conduit5segment1utilityGraphElement);

            conduit5segment1utilityGraphElement.Should().NotBeNull();

            traceResult = utilityNetwork.Graph.TraceSegment(innerConduit5Segment1Id);

            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CO_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CC_1);

            ((UtilityGraphConnectedSegment)traceResult.Downstream.First()).StructureIndex.Should().Be(5);

            // Check that inner conduit 5 segment 2 is connected correctly in graph
            var innerConduit5Segment2Id = spanEquipmentAfterCut.SpanStructures[5].SpanSegments[1].Id;
            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit5Segment2Id, out var conduit5segment2utilityGraphElement);

            traceResult = utilityNetwork.Graph.TraceSegment(innerConduit5Segment2Id);
            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CC_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.HH_10);

        }


        [Fact, Order(3)]
        public async void CutInnerConduit1InHH2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentBeforeCut);

            // Cut inner conduit 1 in HH 2
            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.HH_2,
                spanSegmentsToCut: new Guid[] {
                    spanEquipmentBeforeCut.SpanStructures[0].SpanSegments[0].Id,
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[0].Id
                }
            );

            var cutResult = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentAfterCut);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { sutSpanEquipment })
            );

            equipmentQueryResult.IsSuccess.Should().BeTrue();

            // Assert
            cutResult.IsSuccess.Should().BeTrue();

            // Check that inner conduit 1 is connected correctly in graph
            var innerConduit1Segment1Id = spanEquipmentAfterCut.SpanStructures[1].SpanSegments[0].Id;

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit1Segment1Id, out var conduit1segment1UtilityGraphElement);

            conduit1segment1UtilityGraphElement.Should().NotBeNull();

            var traceResult = utilityNetwork.Graph.TraceSegment(innerConduit1Segment1Id);

            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CO_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.HH_2);

        }


        [Fact, Order(4)]
        public async void ReconnectInnerConduit1InHH2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentBeforeCut);

            // Connect inner conduit 1
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.HH_2,
                spanSegmentsToConnect: new Guid[] {
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[0].Id,
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[1].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentAfterConnect);

            // Assert
            connectResult.IsSuccess.Should().BeTrue();

            // Check that inner conduit 1 is connected correctly in graph
            var innerConduit1Segment1Id = spanEquipmentAfterConnect.SpanStructures[1].SpanSegments[0].Id;

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit1Segment1Id, out var conduit1segment1UtilityGraphElement);

            conduit1segment1UtilityGraphElement.Should().NotBeNull();

            var traceResult = utilityNetwork.Graph.TraceSegment(innerConduit1Segment1Id);

            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CO_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.SP_1);

        }

        [Fact, Order(5)]
        public async void DisconnectInnerConduit1InHH2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentBeforeCut);

            // Connect inner conduit 1
            var disconnectCmd = new DisconnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.HH_2,
                spanSegmentsToDisconnect: new Guid[] {
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[0].Id,
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[1].Id
                }
            );

            var disconnectResult = await _commandDispatcher.HandleAsync<DisconnectSpanSegmentsAtRouteNode, Result>(disconnectCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentAfterConnect);

            // Assert
            disconnectResult.IsSuccess.Should().BeTrue();

            // Check that inner conduit 1 is connected correctly in graph
            var innerConduit1Segment1Id = spanEquipmentAfterConnect.SpanStructures[1].SpanSegments[0].Id;

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit1Segment1Id, out var conduit1segment1UtilityGraphElement);

            conduit1segment1UtilityGraphElement.Should().NotBeNull();

            var traceResult = utilityNetwork.Graph.TraceSegment(innerConduit1Segment1Id);

            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CO_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.HH_2);

        }

        [Fact, Order(6)]
        public async void ReconnectInnerConduit1AgainInHH2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentBeforeCut);

            // Connect inner conduit 1
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.HH_2,
                spanSegmentsToConnect: new Guid[] {
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[0].Id,
                    spanEquipmentBeforeCut.SpanStructures[1].SpanSegments[1].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipmentAfterConnect);

            // Assert
            connectResult.IsSuccess.Should().BeTrue();

            // Check that inner conduit 1 is connected correctly in graph
            var innerConduit1Segment1Id = spanEquipmentAfterConnect.SpanStructures[1].SpanSegments[0].Id;

            utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphElement>(innerConduit1Segment1Id, out var conduit1segment1UtilityGraphElement);

            conduit1segment1UtilityGraphElement.Should().NotBeNull();

            var traceResult = utilityNetwork.Graph.TraceSegment(innerConduit1Segment1Id);

            ((UtilityGraphConnectedTerminal)traceResult.Downstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.CO_1);
            ((UtilityGraphConnectedTerminal)traceResult.Upstream.Last()).NodeOfInterestId.Should().Be(TestRouteNetwork.SP_1);

        }
    }
}

#nullable enable