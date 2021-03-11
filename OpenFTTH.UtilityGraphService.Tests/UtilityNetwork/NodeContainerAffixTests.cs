﻿using DAX.EventProcessing;
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
using System.Linq;
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
            var nodeContainerId = PlaceNodeContainer(TestRouteNetwork.HH_2);

            var testNetwork = new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();

            var testConduitId = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            var testConduit = _eventStore.Projections.Get<UtilityNetworkProjection>().SpanEquipments[testConduitId];

            var affixConduitToContainerCommand = new AffixSpanEquipmentToNodeContainer(
                spanEquipmentOrSegmentId: testConduit.SpanStructures[0].SpanSegments[0].Id,
                nodeContainerId: nodeContainerId,
                nodeContainerIngoingSide: NodeContainerSideEnum.West
            );

            var affixResult = await _commandDispatcher.HandleAsync<AffixSpanEquipmentToNodeContainer, Result>(affixConduitToContainerCommand);

            var equipmentQueryResult = await _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
               new GetEquipmentDetails(new EquipmentIdList() { testConduitId })
            );

            equipmentQueryResult.IsSuccess.Should().BeTrue();

            equipmentQueryResult.Value.SpanEquipment[testConduitId].NodeContainerAffixes.First(n => n.NodeContainerId == nodeContainerId).NodeContainerIngoingSide.Should().Be(NodeContainerSideEnum.West);
        }


        [Fact]
        public async void TestAffixConduitToContainerTwoTimes_ShouldFaild()
        {
            var nodeContainerId = PlaceNodeContainer(TestRouteNetwork.HH_10);

            var testConduits = new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();

            var testConduitId = TestUtilityNetwork.MultiConduit_5x10_HH_1_to_HH_10;

            var testConduit = _eventStore.Projections.Get<UtilityNetworkProjection>().SpanEquipments[testConduitId];

            var affixConduitToContainerCommand = new AffixSpanEquipmentToNodeContainer(
                spanEquipmentOrSegmentId: testConduit.SpanStructures[0].SpanSegments[0].Id,
                nodeContainerId: nodeContainerId,
                nodeContainerIngoingSide: NodeContainerSideEnum.West
            );

            var affixResult1 = await _commandDispatcher.HandleAsync<AffixSpanEquipmentToNodeContainer, Result>(affixConduitToContainerCommand);
            var affixResult2 = await _commandDispatcher.HandleAsync<AffixSpanEquipmentToNodeContainer, Result>(affixConduitToContainerCommand);


            affixResult1.IsSuccess.Should().BeTrue();
            affixResult2.IsSuccess.Should().BeFalse();
            ((AffixSpanEquipmentToNodeContainerError)affixResult2.Errors.First()).Code.Should().Be(AffixSpanEquipmentToNodeContainerErrorCodes.SPAN_EQUIPMENT_ALREADY_AFFIXED_TO_NODE_CONTAINER);

        }


        private Guid PlaceNodeContainer(Guid routeNodeId)
        {
            var specs = new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();

            var nodeContainerId = Guid.NewGuid();
            var nodeOfInterestId = Guid.NewGuid();
            var registerNodeOfInterestCommand = new RegisterNodeOfInterest(nodeOfInterestId, routeNodeId);
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