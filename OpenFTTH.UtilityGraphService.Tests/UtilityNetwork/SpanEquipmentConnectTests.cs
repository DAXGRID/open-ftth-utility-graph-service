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
    [Order(900)]
    public class SpanEquipmentConnectTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public SpanEquipmentConnectTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void TestConnect5x10To3x10ConduitAtCC_1_ShouldSucceed()
        {
            MakeSureTestConduitIsCutAtCC_1();

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutConnectFromSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;
            var sutConnectToSpanEquipment = TestUtilityNetwork.MultiConduit_3x10_CC_1_to_SP_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectFromSpanEquipment, out var sutFromSpanEquipment);
            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectToSpanEquipment, out var sutToSpanEquipment);

            // Connect inner conduit 2 in 5x10 with inner conduit 3 in 3x10
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToConnect: new Guid[] {
                    sutFromSpanEquipment.SpanStructures[4].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[3].SpanSegments[0].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            var fromEquipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { sutConnectFromSpanEquipment })
            );

            var toEquipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
              new GetEquipmentDetails(new EquipmentIdList() { sutConnectToSpanEquipment })
            );

            // Assert
            connectResult.IsSuccess.Should().BeTrue();
            fromEquipmentQueryResult.IsSuccess.Should().BeTrue();
            toEquipmentQueryResult.IsSuccess.Should().BeTrue();

            var fromEquipmentAfterConnect = fromEquipmentQueryResult.Value.SpanEquipment[sutConnectFromSpanEquipment];
            fromEquipmentAfterConnect.SpanStructures[4].SpanSegments[0].ToTerminalId.Should().NotBeEmpty();
            
            var terminalId = fromEquipmentAfterConnect.SpanStructures[4].SpanSegments[0].ToTerminalId;

            var toEquipmentAfterConnect = toEquipmentQueryResult.Value.SpanEquipment[sutConnectToSpanEquipment];
            toEquipmentAfterConnect.SpanStructures[3].SpanSegments[0].FromTerminalId.Should().Be(terminalId);
        }

        [Fact, Order(2)]
        public async void TestConnectMultipleInnerducts5x10To3x10ConduitAtCC_1_ShouldSucceed()
        {
            MakeSureTestConduitIsCutAtCC_1();

            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutConnectFromSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;
            var sutConnectToSpanEquipment = TestUtilityNetwork.MultiConduit_3x10_CC_1_to_SP_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectFromSpanEquipment, out var sutFromSpanEquipment);
            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectToSpanEquipment, out var sutToSpanEquipment);

            // Connect two inner conduits in 5x10 with inner conduits in 3x10
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToConnect: new Guid[] {
                    sutFromSpanEquipment.SpanStructures[1].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[1].SpanSegments[0].Id,
                    sutFromSpanEquipment.SpanStructures[2].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[2].SpanSegments[0].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            var fromEquipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { sutConnectFromSpanEquipment })
            );

            var toEquipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
              new GetEquipmentDetails(new EquipmentIdList() { sutConnectToSpanEquipment })
            );

            // Assert
            connectResult.IsSuccess.Should().BeTrue();
            fromEquipmentQueryResult.IsSuccess.Should().BeTrue();
            toEquipmentQueryResult.IsSuccess.Should().BeTrue();

            var fromEquipmentAfterConnect = fromEquipmentQueryResult.Value.SpanEquipment[sutConnectFromSpanEquipment];
            var toEquipmentAfterConnect = toEquipmentQueryResult.Value.SpanEquipment[sutConnectToSpanEquipment];

            // First connection
            fromEquipmentAfterConnect.SpanStructures[1].SpanSegments[0].ToTerminalId.Should().NotBeEmpty();

            var terminal1Id = fromEquipmentAfterConnect.SpanStructures[1].SpanSegments[0].ToTerminalId;

            toEquipmentAfterConnect.SpanStructures[1].SpanSegments[0].FromTerminalId.Should().Be(terminal1Id);

            // Second connection
            fromEquipmentAfterConnect.SpanStructures[2].SpanSegments[0].ToTerminalId.Should().NotBeEmpty();

            var terminal2Id = fromEquipmentAfterConnect.SpanStructures[2].SpanSegments[0].ToTerminalId;

            toEquipmentAfterConnect.SpanStructures[2].SpanSegments[0].FromTerminalId.Should().Be(terminal2Id);

        }


        [Fact, Order(20)]
        public async void TestConnectAlreadyConnectedSegment_ShouldFail()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutConnectFromSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;
            var sutConnectToSpanEquipment = TestUtilityNetwork.MultiConduit_3x10_CC_1_to_SP_1;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectFromSpanEquipment, out var sutFromSpanEquipment);
            utilityNetwork.TryGetEquipment<SpanEquipment>(sutConnectToSpanEquipment, out var sutToSpanEquipment);

            // Connect inner conduit 2 in 5x10 with inner conduit 1 in 3x10
            var connectCmd = new ConnectSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToConnect: new Guid[] {
                    sutFromSpanEquipment.SpanStructures[2].SpanSegments[0].Id,
                    sutToSpanEquipment.SpanStructures[1].SpanSegments[0].Id
                }
            );

            var connectResult = await _commandDispatcher.HandleAsync<ConnectSpanSegmentsAtRouteNode, Result>(connectCmd);

            // Assert
            connectResult.IsFailed.Should().BeTrue();
            ((ConnectSpanSegmentsAtRouteNodeError)connectResult.Errors.First()).Code.Should().Be(ConnectSpanSegmentsAtRouteNodeErrorCodes.SPAN_SEGMENT_ALREADY_CONNECTED);
        }



        private async void MakeSureTestConduitIsCutAtCC_1()
        {
            var utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();

            var sutSpanEquipment = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            utilityNetwork.TryGetEquipment<SpanEquipment>(sutSpanEquipment, out var spanEquipment);

            // Cut segments in structure 1 (the outer conduit and second inner conduit)
            var cutCmd = new CutSpanSegmentsAtRouteNode(
                routeNodeId: TestRouteNetwork.CC_1,
                spanSegmentsToCut: new Guid[] {
                    spanEquipment.SpanStructures[0].SpanSegments[0].Id,
                    spanEquipment.SpanStructures[1].SpanSegments[0].Id,
                    spanEquipment.SpanStructures[2].SpanSegments[0].Id,
                    spanEquipment.SpanStructures[3].SpanSegments[0].Id,
                    spanEquipment.SpanStructures[4].SpanSegments[0].Id
                }
            );

            await _commandDispatcher.HandleAsync<CutSpanSegmentsAtRouteNode, Result>(cutCmd);
        }
    }
}

#nullable enable
