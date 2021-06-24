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
    [Order(3000)]
    public class SpanEquipmentRevertCutTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public SpanEquipmentRevertCutTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void TestRevertCut5x10ConduitAtHH_2_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var sutSpanEquipmentBeforeUncut);

            // Connect outer conduits which will revert cut
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(Guid.NewGuid(), new UserContext("test", Guid.Empty),
                routeNodeId: TestRouteNetwork.HH_2,
                spanSegmentsToConnect: new Guid[] {
                    sutSpanEquipmentBeforeUncut.SpanStructures[0].SpanSegments[0].Id,
                    sutSpanEquipmentBeforeUncut.SpanStructures[0].SpanSegments[1].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var sutSpanEquipmentAfterUncut);

            // Assert
            connectResult.IsSuccess.Should().BeTrue();

            sutSpanEquipmentAfterUncut.NodesOfInterestIds.Length.Should().Be(sutSpanEquipmentBeforeUncut.NodesOfInterestIds.Length - 1);

            // Structure 0 and 1 should be "glued" together
            sutSpanEquipmentAfterUncut.SpanStructures[0].SpanSegments.Length.Should().Be(2);
            sutSpanEquipmentAfterUncut.SpanStructures[0].SpanSegments.Length.Should().Be(2);

            // Assert that segments in structure 0 (outer span) is correctly modified
            sutSpanEquipmentAfterUncut.SpanStructures[0].SpanSegments[0].FromNodeOfInterestIndex.Should().Be(0);
            sutSpanEquipmentAfterUncut.SpanStructures[0].SpanSegments[0].ToNodeOfInterestIndex.Should().Be(1);

            sutSpanEquipmentAfterUncut.SpanStructures[0].SpanSegments[1].FromNodeOfInterestIndex.Should().Be(1);
            sutSpanEquipmentAfterUncut.SpanStructures[0].SpanSegments[1].ToNodeOfInterestIndex.Should().Be(2);

            // Assert that segments in structure 1 is correctly modified
            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[0].FromNodeOfInterestIndex.Should().Be(0);
            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[0].ToNodeOfInterestIndex.Should().Be(1);

            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[0].FromTerminalId.Should().Be(sutSpanEquipmentBeforeUncut.SpanStructures[1].SpanSegments[0].FromTerminalId);
            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[0].ToTerminalId.Should().Be(sutSpanEquipmentBeforeUncut.SpanStructures[1].SpanSegments[1].ToTerminalId);

            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[1].FromNodeOfInterestIndex.Should().Be(1);
            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[1].ToNodeOfInterestIndex.Should().Be(2);

            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[1].FromTerminalId.Should().Be(sutSpanEquipmentBeforeUncut.SpanStructures[1].SpanSegments[2].FromTerminalId);
            sutSpanEquipmentAfterUncut.SpanStructures[1].SpanSegments[1].ToTerminalId.Should().Be(sutSpanEquipmentBeforeUncut.SpanStructures[1].SpanSegments[2].ToTerminalId);

            // Assert that node indexes of the rest is correctly modified
            for (int i = 2; i < sutSpanEquipmentAfterUncut.SpanStructures.Length; i++)
            {
                sutSpanEquipmentAfterUncut.SpanStructures[i].SpanSegments.Length.Should().Be(2);

                sutSpanEquipmentAfterUncut.SpanStructures[i].SpanSegments[0].FromNodeOfInterestIndex.Should().Be(0);
                sutSpanEquipmentAfterUncut.SpanStructures[i].SpanSegments[0].ToNodeOfInterestIndex.Should().Be(1);

                sutSpanEquipmentAfterUncut.SpanStructures[i].SpanSegments[1].FromNodeOfInterestIndex.Should().Be(1);
                sutSpanEquipmentAfterUncut.SpanStructures[i].SpanSegments[1].ToNodeOfInterestIndex.Should().Be(2);
            }

            // Check if an event is published to the notification.utility-network topic having an idlist containing the span equipment id we just created
            var utilityNetworkNotifications = _externalEventProducer.GetMessagesByTopic("notification.utility-network").OfType<RouteNetworkElementContainedEquipmentUpdated>();
            var utilityNetworkUpdatedEvent = utilityNetworkNotifications.First(n => n.Category == "EquipmentModification.RevertCut" && n.IdChangeSets != null && n.IdChangeSets.Any(i => i.IdList.Any(i => i == sutSpanEquipmentId)));
            utilityNetworkUpdatedEvent.AffectedRouteNetworkElementIds.Should().Contain(TestRouteNetwork.HH_2);

            // Check traces
            var traceQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { sutSpanEquipmentId })
               {
                   EquipmentDetailsFilter = new EquipmentDetailsFilterOptions()
                   {
                       IncludeRouteNetworkTrace = true
                   }
               }
           );

            traceQueryResult.IsSuccess.Should().BeTrue();
                        
            var routeNetworkTraces = traceQueryResult.Value.RouteNetworkTraces;

            routeNetworkTraces.Count.Should().Be(5);
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CO_1 && t.ToRouteNodeId == TestRouteNetwork.CC_1 && t.RouteSegmentIds.Length == 3).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.HH_1 && t.ToRouteNodeId == TestRouteNetwork.HH_10 && t.RouteSegmentIds.Length == 3).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.HH_1 && t.ToRouteNodeId == TestRouteNetwork.CC_1 && t.RouteSegmentIds.Length == 2).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CO_1 && t.ToRouteNodeId == TestRouteNetwork.SP_1 && t.RouteSegmentIds.Length == 4).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CC_1 && t.ToRouteNodeId == TestRouteNetwork.HH_10 && t.RouteSegmentIds.Length == 1).Should().BeTrue();

        }

        [Fact, Order(10)]
        public async void TestRevertCut5x10ConduitAtHH_2Again_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var sutSpanEquipmentBeforeUncut);

            // Connect outer conduits which will revert cut
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(Guid.NewGuid(), new UserContext("test", Guid.Empty),
                routeNodeId: TestRouteNetwork.HH_2,
                spanSegmentsToConnect: new Guid[] {
                    sutSpanEquipmentBeforeUncut.SpanStructures[0].SpanSegments[0].Id,
                    sutSpanEquipmentBeforeUncut.SpanStructures[0].SpanSegments[1].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var sutSpanEquipmentAfterUncut);

            // Assert
            connectResult.IsFailed.Should().BeTrue();

            ((ConnectSpanSegmentsAtRouteNodeError)connectResult.Errors.First()).Code.Should().Be(ConnectSpanSegmentsAtRouteNodeErrorCodes.CANNOT_REVERT_SPAN_EQUIPMENT_CUT_DUE_TO_NOT_BEING_CUT);
        }




        [Fact, Order(11)]
        public async void TestRevertCut5x10ConduitAtCC_1_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var sutSpanEquipmentBeforeUncut);

            // Connect outer conduits which will revert cut
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(Guid.NewGuid(), new UserContext("test", Guid.Empty),
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToConnect: new Guid[] {
                    sutSpanEquipmentBeforeUncut.SpanStructures[0].SpanSegments[0].Id,
                    sutSpanEquipmentBeforeUncut.SpanStructures[0].SpanSegments[1].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId, out var sutSpanEquipmentAfterUncut);

            // Assert
            connectResult.IsFailed.Should().BeTrue();

            ((ConnectSpanSegmentsAtRouteNodeError)connectResult.Errors.First()).Code.Should().Be(ConnectSpanSegmentsAtRouteNodeErrorCodes.CANNOT_REVERT_SPAN_EQUIPMENT_CUT_DUE_TO_CONNECTED_SEGMENT);
        }
    }
}

#nullable enable
