using DAX.EventProcessing;
using FluentAssertions;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Commands;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.TestData;
using OpenFTTH.UtilityGraphService.API.Commands;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using Xunit;

#nullable disable

namespace OpenFTTH.UtilityGraphService.Tests.UtilityNetwork
{
    public class NodeContainerAffixTests
    {
        private readonly IEventStore _eventStore;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly FakeExternalEventProducer _externalEventProducer;

        public NodeContainerAffixTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IExternalEventProducer externalEventProducer)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
            _externalEventProducer = (FakeExternalEventProducer)externalEventProducer;
        }
        
        [Fact]
        public async void TestAffixConduitToContainer_ShouldSucceed()
        {
            var nodeContainerId = PlaceNodeContainerInHH_2();

            var testConduits = new TestConduits(_commandDispatcher, _queryDispatcher).Run();

            var testConduitId = TestConduits.MultiConduit_5x10_HH_1_to_HH_10;

            var testConduit = _eventStore.Projections.Get<UtilityGraphProjection>().SpanEquipments[testConduitId];

            var affixConduitToContainerCommand = new AffixSpanEquipmentToNodeContainer(
                spanSegmentId: testConduit.SpanStructures[0].SpanSegments[0].Id,
                nodeContainerId: nodeContainerId,
                nodeContainerIngoingSide: NodeContainerSideEnum.Vest
            );

            var affixResult = await _commandDispatcher.HandleAsync<AffixSpanEquipmentToNodeContainer, Result>(affixConduitToContainerCommand);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { testConduitId })
            );

            equipmentQueryResult.IsSuccess.Should().BeTrue();

            equipmentQueryResult.Value.SpanEquipment[testConduitId].NodeContainerAffixes.Length.Should().Be(1);
            equipmentQueryResult.Value.SpanEquipment[testConduitId].NodeContainerAffixes[0].NodeContainerId.Should().Be(nodeContainerId);
            equipmentQueryResult.Value.SpanEquipment[testConduitId].NodeContainerAffixes[0].NodeContainerIngoingSide.Should().Be(NodeContainerSideEnum.Vest);

        }


        private Guid PlaceNodeContainerInHH_2()
        {
            var specs = new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();

            var nodeContainerId = Guid.NewGuid();
            var nodeOfInterestId = Guid.NewGuid();
            var registerNodeOfInterestCommand = new RegisterNodeOfInterest(nodeOfInterestId, TestRouteNetwork.HH_2);
            var registerNodeOfInterestCommandResult = _commandDispatcher.HandleAsync<RegisterNodeOfInterest, Result<RouteNetworkInterest>>(registerNodeOfInterestCommand).Result;

            var placeNodeContainerCommand = new PlaceNodeContainerInRouteNetwork(nodeContainerId, TestSpecifications.Conduit_Closure_Emtelle_Branch_Box, registerNodeOfInterestCommandResult.Value)
            {
                ManufacturerId = TestSpecifications.Manu_Emtelle
            };

            var placeNodeContainerResult = _commandDispatcher.HandleAsync<PlaceNodeContainerInRouteNetwork, Result>(placeNodeContainerCommand).Result;

            return nodeContainerId;
        }
    }
}

#nullable enable
