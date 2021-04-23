using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
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
    [Order(5000)]
    public class TraceTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public TraceTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void Trace5x10_HH_1_to_HH_10_ShouldSucceed()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipmentId1 = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;
            var sutSpanEquipmentId2 = TestUtilityNetwork.FlexConduit_40_Red_CC_1_to_SP_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId1, out var sutSpanEquipment1);
            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipmentId2, out var sutSpanEquipment2);

            var traceQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                new GetEquipmentDetails(new EquipmentIdList() { sutSpanEquipmentId1, sutSpanEquipmentId2 })
                {
                    EquipmentDetailsFilter = new EquipmentDetailsFilterOptions()
                    {
                        IncludeRouteNetworkTrace = true
                    }
                }
            );

            // Assert
            traceQueryResult.IsSuccess.Should().BeTrue();

            // Check route network traces
            var routeNetworkTraces = traceQueryResult.Value.RouteNetworkTraces;

            routeNetworkTraces.Count.Should().Be(5);
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CO_1 && t.ToRouteNodeId == TestRouteNetwork.CC_1 && t.RouteSegmentIds.Length == 3).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CO_1 && t.ToRouteNodeId == TestRouteNetwork.SP_1 && t.RouteSegmentIds.Length == 4).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CC_1 && t.ToRouteNodeId == TestRouteNetwork.SP_1 && t.RouteSegmentIds.Length == 1).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.CC_1 && t.ToRouteNodeId == TestRouteNetwork.HH_10 && t.RouteSegmentIds.Length == 1).Should().BeTrue();
            routeNetworkTraces.Any(t => t.FromRouteNodeId == TestRouteNetwork.HH_1 && t.ToRouteNodeId == TestRouteNetwork.HH_10 && t.RouteSegmentIds.Length == 3).Should().BeTrue();

            // Check sut 1 trace refs
            var spanEquipment1TraceRefs = traceQueryResult.Value.SpanEquipment[sutSpanEquipmentId1].RouteNetworkTraceRefs;
            spanEquipment1TraceRefs.Length.Should().Be(8);
            spanEquipment1TraceRefs.Count(tr => tr.SpanEquipmentOrSegmentId == sutSpanEquipmentId1).Should().Be(1);

            // Check that all segments that is connected has a reference to a trace
            foreach (var spanStructure in sutSpanEquipment1.SpanStructures)
                foreach (var spanSegment in spanStructure.SpanSegments.Where(s => s.FromTerminalId != Guid.Empty || s.ToTerminalId != Guid.Empty))
                    spanEquipment1TraceRefs.Count(tr => tr.SpanEquipmentOrSegmentId == spanSegment.Id).Should().Be(1);

            // Check sut 2 trace refs
            var spanEquipment2TraceRefs = traceQueryResult.Value.SpanEquipment[sutSpanEquipmentId2].RouteNetworkTraceRefs;
            spanEquipment2TraceRefs.Length.Should().Be(1);
            spanEquipment2TraceRefs.Count(tr => tr.SpanEquipmentOrSegmentId == sutSpanEquipmentId2).Should().Be(1);

            // Check that all segments that is connected has a reference to a trace
            foreach (var spanStructure in sutSpanEquipment2.SpanStructures)
                foreach (var spanSegment in spanStructure.SpanSegments.Where(s => s.FromTerminalId != Guid.Empty || s.ToTerminalId != Guid.Empty))
                    spanEquipment1TraceRefs.Count(tr => tr.SpanEquipmentOrSegmentId == spanSegment.Id).Should().Be(1);
        }
    }
}

#nullable enable
