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

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    public class SpanEquipmentCutTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public SpanEquipmentCutTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact]
        public async void TestCutOuterContainerAtCC_1_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipment);

            // Cut segments in structure 1 (the outer conduit and second inner conduit)
            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToCut: new Guid[] { 
                    spanEquipment.SpanStructures[0].SpanSegments[0].Id,
                    spanEquipment.SpanStructures[2].SpanSegments[0].Id
                }
            );

            var cutResult = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { sutSpanEquipment })
            );

            equipmentQueryResult.IsSuccess.Should().BeTrue();

            // Assert
            cutResult.IsSuccess.Should().BeTrue();

            var spanEquipmentAfterCut = equipmentQueryResult.Value.SpanEquipment[sutSpanEquipment];

            spanEquipmentAfterCut.NodesOfInterestIds.Length.Should().Be(3);
            spanEquipmentAfterCut.NodesOfInterestIds[0].Should().Be(spanEquipment.NodesOfInterestIds[0]);
            spanEquipmentAfterCut.NodesOfInterestIds[1].Should().Be(TestRouteNetwork.CC_1);
            spanEquipmentAfterCut.NodesOfInterestIds[2].Should().Be(spanEquipment.NodesOfInterestIds[1]);

            // Outer conduit
            spanEquipmentAfterCut.SpanStructures[0].SpanSegments.Length.Should().Be(2);

            // First inner conduit (should not be cut)
            spanEquipmentAfterCut.SpanStructures[1].SpanSegments.Length.Should().Be(1);

            // Second inner conduit
            spanEquipmentAfterCut.SpanStructures[2].SpanSegments.Length.Should().Be(2);

            // Check if an event is published to the notification.utility-network topic having an idlist containing the span equipment id we just created
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutSpanEquipment)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(TestRouteNetwork.CC_1);
        }

        [Fact]
        public async void TryCutWhenSpanEquipmentIsNotAffixedToNodeContainer_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            utilityNetwork.TryGetEquipment<SpanEquipment>(TestUtilityNetwork.MultiConduit_3x10_CC_1_to_HH_11, out var spanEquipment);

            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.J_2,
                spanSegmentsToCut: new Guid[] { spanEquipment.SpanStructures[1].SpanSegments[0].Id }
            );

            var cutResult = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);

            cutResult.IsFailed.Should().BeTrue();
            ((CutSpanSegmentsAtRouteNodeError)cutResult.Errors.First()).Code.Should().Be(CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_EQUIPMENT_NOT_AFFIXED_TO_NODE_CONTAINER);
        }

        [Fact]
        public async void TryCutAtSpanEquipmentEnd_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            utilityNetwork.TryGetEquipment<SpanEquipment>(TestUtilityNetwork.MultiConduit_3x10_CC_1_to_HH_11, out var spanEquipment);

            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToCut: new Guid[] { spanEquipment.SpanStructures[1].SpanSegments[0].Id }
            );

            var cutResult = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);

            cutResult.IsFailed.Should().BeTrue();
            ((CutSpanSegmentsAtRouteNodeError)cutResult.Errors.First()).Code.Should().Be(CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_EQUIPMENT_CANNOT_BE_CUT_AT_ENDS);
        }

        [Fact]
        public async void TryCutAtSpanSegmentThatDontExist_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            utilityNetwork.TryGetEquipment<SpanEquipment>(TestUtilityNetwork.MultiConduit_3x10_CC_1_to_HH_11, out var spanEquipment);

            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.J_1,
                spanSegmentsToCut: new Guid[] { 
                    Guid.NewGuid() // This one defiantly will not exists
                }
            );

            var cutResult = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);

            cutResult.IsFailed.Should().BeTrue();
            ((CutSpanSegmentsAtRouteNodeError)cutResult.Errors.First()).Code.Should().Be(CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND);
        }

        [Fact]
        public async void TryCutSameSpanSegmentTwoTimes_ShouldFailSecondTime()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            utilityNetwork.TryGetEquipment<SpanEquipment>(TestUtilityNetwork.MultiConduit_3x10_CC_1_to_HH_11, out var spanEquipment1);

            var cutCmd1 = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.J_1,
                spanSegmentsToCut: new Guid[] {
                    spanEquipment1.SpanStructures[0].SpanSegments[0].Id
                }
            );

            var cutResult1 = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd1);

            utilityNetwork.TryGetEquipment<SpanEquipment>(TestUtilityNetwork.MultiConduit_3x10_CC_1_to_HH_11, out var spanEquipment2);

            var cutCmd2 = new CutSpanSegmentsAtRouteNode(
               routeNodeId: TestRouteNetwork.J_1,
               spanSegmentsToCut: new Guid[] {
                    spanEquipment2.SpanStructures[0].SpanSegments[0].Id
               }
            );

            var cutResult2 = await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd2);

            cutResult1.IsSuccess.Should().BeTrue();
            cutResult2.IsFailed.Should().BeTrue();
            ((CutSpanSegmentsAtRouteNodeError)cutResult2.Errors.First()).Code.Should().Be(CutSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_NOT_FOUND);
        }
    }
}

#nullable enable
