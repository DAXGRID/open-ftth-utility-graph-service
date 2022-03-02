using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.Events.Core.Infos;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.TestData;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    [Order(8301)]
    public class T8310_CableInConduitMoveTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;
        private ConduitTestUtilityNetwork _conduitTestUtilityNetwork;

        public T8310_CableInConduitMoveTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            _conduitTestUtilityNetwork = new ConduitTestUtilityNetwork(_eventStore, _commandDispatcher, _queryDispatcher).Run();
        }

        [Fact, Order(1)]
        public async void Move_Conduit_N2_N3_To_N2_N6_N7_N3_ShouldSucceed()
        {
            var sutCable = _conduitTestUtilityNetwork.PlaceCableDirectlyInRouteNetwork("K8310_1", TestSpecifications.FiberCable_12Fiber,
                new Guid[] { ConduitTestUtilityNetwork.S1, ConduitTestUtilityNetwork.S2, ConduitTestUtilityNetwork.S3 });

            var sutConduitId = ConduitTestUtilityNetwork.Conduit_N2_N3_1;

            // Move cable into Conduit N2_N3_1
            var cableAfterFirstAffix = _conduitTestUtilityNetwork.AffixCableToSingleConduit(ConduitTestUtilityNetwork.N2, sutCable.Id, sutConduitId);

            var moveCmd = new MoveSpanEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutConduitId, new RouteNetworkElementIdList() { ConduitTestUtilityNetwork.S8, ConduitTestUtilityNetwork.S5, ConduitTestUtilityNetwork.S9 });

            var moveCmdResult = await _commandDispatcher.HandleAsync<MoveSpanEquipment, Result>(moveCmd);

            moveCmdResult.IsSuccess.Should().BeTrue();

            var sutCableWalkOfInterestAfterConduitMove = _conduitTestUtilityNetwork.GetWalkOfInterest(sutCable.WalkOfInterestId);

            sutCableWalkOfInterestAfterConduitMove.Count.Should().Be(11);

            sutCableWalkOfInterestAfterConduitMove[0].Should().Be(ConduitTestUtilityNetwork.N1);
            sutCableWalkOfInterestAfterConduitMove[1].Should().Be(ConduitTestUtilityNetwork.S1);
            sutCableWalkOfInterestAfterConduitMove[2].Should().Be(ConduitTestUtilityNetwork.N2);
            sutCableWalkOfInterestAfterConduitMove[3].Should().Be(ConduitTestUtilityNetwork.S8);
            sutCableWalkOfInterestAfterConduitMove[4].Should().Be(ConduitTestUtilityNetwork.N6);
            sutCableWalkOfInterestAfterConduitMove[5].Should().Be(ConduitTestUtilityNetwork.S5);
            sutCableWalkOfInterestAfterConduitMove[6].Should().Be(ConduitTestUtilityNetwork.N7);
            sutCableWalkOfInterestAfterConduitMove[7].Should().Be(ConduitTestUtilityNetwork.S9);
            sutCableWalkOfInterestAfterConduitMove[8].Should().Be(ConduitTestUtilityNetwork.N3);
            sutCableWalkOfInterestAfterConduitMove[9].Should().Be(ConduitTestUtilityNetwork.S3);
            sutCableWalkOfInterestAfterConduitMove[10].Should().Be(ConduitTestUtilityNetwork.N4);
        }

        [Fact, Order(3)]
        public async void Affix_K8319_2_To_Conduit_N2_N6_N7_N3_And_Move_Back_To_N2_N3_ShouldSucceed()
        {
            var sutCable = _conduitTestUtilityNetwork.PlaceCableDirectlyInRouteNetwork("K8310_2", TestSpecifications.FiberCable_12Fiber,
                new Guid[] { ConduitTestUtilityNetwork.S3, ConduitTestUtilityNetwork.S2, ConduitTestUtilityNetwork.S1 });

            var sutConduitId = ConduitTestUtilityNetwork.Conduit_N2_N3_1;

            // Move cable into Conduit N2_N3_1
            var cableAfterFirstAffix = _conduitTestUtilityNetwork.AffixCableToSingleConduit(ConduitTestUtilityNetwork.N2, sutCable.Id, sutConduitId);

            // Move conduit back to N2_N3
            var moveCmd = new MoveSpanEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutConduitId, new RouteNetworkElementIdList() { ConduitTestUtilityNetwork.S2 });

            var moveCmdResult = await _commandDispatcher.HandleAsync<MoveSpanEquipment, Result>(moveCmd);

            moveCmdResult.IsSuccess.Should().BeTrue();


            var sutCableWalkOfInterestAfterConduitMove = _conduitTestUtilityNetwork.GetWalkOfInterest(sutCable.WalkOfInterestId);

            sutCableWalkOfInterestAfterConduitMove.Count.Should().Be(7);

            sutCableWalkOfInterestAfterConduitMove[6].Should().Be(ConduitTestUtilityNetwork.N1);
            sutCableWalkOfInterestAfterConduitMove[5].Should().Be(ConduitTestUtilityNetwork.S1);
            sutCableWalkOfInterestAfterConduitMove[4].Should().Be(ConduitTestUtilityNetwork.N2);
            sutCableWalkOfInterestAfterConduitMove[3].Should().Be(ConduitTestUtilityNetwork.S2);
            sutCableWalkOfInterestAfterConduitMove[2].Should().Be(ConduitTestUtilityNetwork.N3);
            sutCableWalkOfInterestAfterConduitMove[1].Should().Be(ConduitTestUtilityNetwork.S3);
            sutCableWalkOfInterestAfterConduitMove[0].Should().Be(ConduitTestUtilityNetwork.N4);
        }

        [Fact, Order(4)]
        public async void Affix_K8319_3_To_Conduit_N2_N3_And_Move_To_N2_N6_N7_N3_ShouldSucceed()
        {
            var sutCable = _conduitTestUtilityNetwork.PlaceCableDirectlyInRouteNetwork("K8310_3", TestSpecifications.FiberCable_12Fiber,
                new Guid[] { ConduitTestUtilityNetwork.S2 });

            var sutConduitId = ConduitTestUtilityNetwork.Conduit_N2_N3_1;

            // Affix cable into Conduit N2_N3_1
            var cableAfterFirstAffix = _conduitTestUtilityNetwork.AffixCableToSingleConduit(ConduitTestUtilityNetwork.N2, sutCable.Id, sutConduitId);

            // Move cable to 
            var moveCmd = new MoveSpanEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutConduitId, new RouteNetworkElementIdList() { ConduitTestUtilityNetwork.S8, ConduitTestUtilityNetwork.S5, ConduitTestUtilityNetwork.S9 });

            var moveCmdResult = await _commandDispatcher.HandleAsync<MoveSpanEquipment, Result>(moveCmd);

            moveCmdResult.IsSuccess.Should().BeTrue();

            var sutCableWalkOfInterestAfterConduitMove = _conduitTestUtilityNetwork.GetWalkOfInterest(sutCable.WalkOfInterestId);

            sutCableWalkOfInterestAfterConduitMove.Count.Should().Be(7);

            sutCableWalkOfInterestAfterConduitMove[0].Should().Be(ConduitTestUtilityNetwork.N2);
            sutCableWalkOfInterestAfterConduitMove[1].Should().Be(ConduitTestUtilityNetwork.S8);
            sutCableWalkOfInterestAfterConduitMove[2].Should().Be(ConduitTestUtilityNetwork.N6);
            sutCableWalkOfInterestAfterConduitMove[3].Should().Be(ConduitTestUtilityNetwork.S5);
            sutCableWalkOfInterestAfterConduitMove[4].Should().Be(ConduitTestUtilityNetwork.N7);
            sutCableWalkOfInterestAfterConduitMove[5].Should().Be(ConduitTestUtilityNetwork.S9);
            sutCableWalkOfInterestAfterConduitMove[6].Should().Be(ConduitTestUtilityNetwork.N3);
        
        }



        [Fact, Order(100)]
        public async void Move_Conduit_N2_N3_To_N2_N6_ShouldFail()
        {
            var sutConduitId = ConduitTestUtilityNetwork.Conduit_N2_N3_1;

            var moveCmd = new MoveSpanEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutConduitId, new RouteNetworkElementIdList() { ConduitTestUtilityNetwork.S8 });

            var moveCmdResult = await _commandDispatcher.HandleAsync<MoveSpanEquipment, Result>(moveCmd);

            moveCmdResult.IsFailed.Should().BeTrue();
        }


        [Fact, Order(101)]
        public async void Move_Conduit_N2_N3_To_N2_N4_ShouldFail()
        {
            var sutConduitId = ConduitTestUtilityNetwork.Conduit_N2_N3_1;

            var moveCmd = new MoveSpanEquipment(Guid.NewGuid(), new UserContext("test", Guid.Empty), sutConduitId, new RouteNetworkElementIdList() { ConduitTestUtilityNetwork.S2, ConduitTestUtilityNetwork.S3 });

            var moveCmdResult = await _commandDispatcher.HandleAsync<MoveSpanEquipment, Result>(moveCmd);

            moveCmdResult.IsFailed.Should().BeTrue();
        }

    }
}
