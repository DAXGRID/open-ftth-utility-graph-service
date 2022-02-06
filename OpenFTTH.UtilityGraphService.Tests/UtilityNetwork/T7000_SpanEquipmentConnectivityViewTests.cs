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
    [Order(7000)]
    public class T7000_SpanEquipmentConnectivityViewTests
    {
        private IEventStore _eventStore;
        private ICommandDispatcher _commandDispatcher;
        private IQueryDispatcher _queryDispatcher;

        public T7000_SpanEquipmentConnectivityViewTests(IEventStore eventStore, ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;

            new TestSpecifications(_commandDispatcher, _queryDispatcher).Run();
            new TestUtilityNetwork(_commandDispatcher, _queryDispatcher).Run();
        }


        [Fact, Order(1)]
        public async void GetSpanEquipmentConnectivityViewOnCable_ShouldSucceed()
        {
            var sutRouteNetworkElementId = TestRouteNetwork.CC_1;

            var cable = FindSpanEquipmentRelatedToRouteNetworkElementByName(sutRouteNetworkElementId, "K666");

            var connectivityTrace = new GetSpanEquipmentConnectivityView(sutRouteNetworkElementId, new Guid[] { cable.Id });

            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetSpanEquipmentConnectivityView, Result<SpanEquipmentAZConnectivityViewModel>>(
                connectivityTrace
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;
        }

        [Fact, Order(2)]
        public async void GetSpanEquipmentConnectivityViewOnConduit_ShouldSucceed()
        {
            var sutRouteNetworkElementId = TestRouteNetwork.CC_1;

            var conduit = FindSpanEquipmentRelatedToRouteNetworkElementById(sutRouteNetworkElementId, TestUtilityNetwork.MultiConduit_10x10_HH_1_to_HH_10);

            var connectivityTrace = new GetSpanEquipmentConnectivityView(sutRouteNetworkElementId, new Guid[] { conduit.Id });

            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetSpanEquipmentConnectivityView, Result<SpanEquipmentAZConnectivityViewModel>>(
                connectivityTrace
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;
        }


        [Fact, Order(3)]
        public async void GetSpanEquipmentConnectivityViewOnCableK69373563_ShouldSucceed()
        {
            var sutRouteNetworkElementId = TestRouteNetwork.CO_1;

            var cable = FindSpanEquipmentRelatedToRouteNetworkElementByName(sutRouteNetworkElementId, "K69373563");

            var connectivityTrace = new GetSpanEquipmentConnectivityView(sutRouteNetworkElementId, new Guid[] { cable.Id });

            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetSpanEquipmentConnectivityView, Result<SpanEquipmentAZConnectivityViewModel>>(
                connectivityTrace
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;

            var fiber2Line = viewModel.SpanEquipments.First().Lines[1];
        }


        [Fact, Order(4)]
        public async void GetTerminalEquipmentConnectivityViewOnCO1_ShouldSucceed()
        {

            // Get faces
            var connectivityFaceQuery = new GetConnectivityFaces(TestRouteNetwork.CO_1);

            var connectivityFaceQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            connectivityFaceQueryResult.IsSuccess.Should().BeTrue();

            var connectivityFaces = connectivityFaceQueryResult.Value;

            connectivityFaces.Count(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment).Should().BeGreaterThan(0);
            connectivityFaces.Count(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.SpanEquipment).Should().BeGreaterThan(0);

            var terminalEquipmentFace = connectivityFaces.First(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment);

            var connectivityTrace = new GetTerminalEquipmentConnectivityView(TestRouteNetwork.CO_1, terminalEquipmentFace.EquipmentId);

            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetTerminalEquipmentConnectivityView, Result<TerminalEquipmentAZConnectivityViewModel>>(
                connectivityTrace
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;
        }


        [Fact, Order(4)]
        public async void GetTerminalEquipmentConnectivityViewOnCC1_ShouldSucceed()
        {

            // Get faces
            var connectivityFaceQuery = new GetConnectivityFaces(TestRouteNetwork.CC_1);

            var connectivityFaceQueryResult = await _queryDispatcher.HandleAsync<GetConnectivityFaces, Result<List<ConnectivityFace>>>(
                connectivityFaceQuery
            );

            connectivityFaceQueryResult.IsSuccess.Should().BeTrue();

            var connectivityFaces = connectivityFaceQueryResult.Value;

            connectivityFaces.Count(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment).Should().BeGreaterThan(0);
            connectivityFaces.Count(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.SpanEquipment).Should().BeGreaterThan(0);

            var terminalEquipmentFace = connectivityFaces.First(f => f.EquipmentKind == ConnectivityEquipmentKindEnum.TerminalEquipment);

            var connectivityTrace = new GetTerminalEquipmentConnectivityView(TestRouteNetwork.CC_1, terminalEquipmentFace.EquipmentId);

            // Act
            var connectivityQueryResult = await _queryDispatcher.HandleAsync<GetTerminalEquipmentConnectivityView, Result<TerminalEquipmentAZConnectivityViewModel>>(
                connectivityTrace
            );

            // Assert
            connectivityQueryResult.IsSuccess.Should().BeTrue();

            var viewModel = connectivityQueryResult.Value;


        }





        private SpanEquipment? FindSpanEquipmentRelatedToRouteNetworkElementByName(Guid routeNetworkElementId, string spanEquipmentName)
        {
            var routeNetworkQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
              new GetRouteNetworkDetails(new RouteNetworkElementIdList() { routeNetworkElementId })
              {
                  RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementAndInterestObjects
              }
            ).Result;

            InterestIdList interestIdList = new InterestIdList();
            foreach (var interestRel in routeNetworkQueryResult.Value.RouteNetworkElements[routeNetworkElementId].InterestRelations)
            {
                interestIdList.Add(interestRel.RefId);
            }

            var equipmentQueryResult = _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                    new GetEquipmentDetails(interestIdList)
                    {
                        EquipmentDetailsFilter = new EquipmentDetailsFilterOptions() { IncludeRouteNetworkTrace = true }
                    }
                ).Result;

            foreach (var spanEquipment in equipmentQueryResult.Value.SpanEquipment)
            {
                if (spanEquipment.Name == spanEquipmentName)
                    return spanEquipment;
            }

            return null;
        }

        private SpanEquipment? FindSpanEquipmentRelatedToRouteNetworkElementById(Guid routeNetworkElementId, Guid spanEquipmentId)
        {
            var routeNetworkQueryResult = _queryDispatcher.HandleAsync<GetRouteNetworkDetails, Result<GetRouteNetworkDetailsResult>>(
              new GetRouteNetworkDetails(new RouteNetworkElementIdList() { routeNetworkElementId })
              {
                  RelatedInterestFilter = RelatedInterestFilterOptions.ReferencesFromRouteElementAndInterestObjects
              }
            ).Result;

            InterestIdList interestIdList = new InterestIdList();
            foreach (var interestRel in routeNetworkQueryResult.Value.RouteNetworkElements[routeNetworkElementId].InterestRelations)
            {
                interestIdList.Add(interestRel.RefId);
            }

            var equipmentQueryResult = _queryDispatcher.HandleAsync<GetEquipmentDetails, Result<GetEquipmentDetailsResult>>(
                    new GetEquipmentDetails(interestIdList)
                    {
                        EquipmentDetailsFilter = new EquipmentDetailsFilterOptions() { IncludeRouteNetworkTrace = true }
                    }
                ).Result;

            foreach (var spanEquipment in equipmentQueryResult.Value.SpanEquipment)
            {
                if (spanEquipment.Id == spanEquipmentId)
                    return spanEquipment;
            }

            return null;
        }
    }
}
