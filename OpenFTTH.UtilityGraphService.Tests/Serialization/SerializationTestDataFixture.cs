﻿using OpenFTTH.UtilityGraphService.API.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using System;

namespace OpenFTTH.UtilityGraphService.Tests.Fixtures
{

    public class SerializationTestDataFixture : IDisposable
    {
        readonly GetRelatedEquipmentQueryResult _fullyPopulatedGetRelatedEquipmentsQueryResult = CreateFullyPopulatedGetRelatedEquipmentsQueryResult;

        public GetRelatedEquipmentQueryResult FullyPopulatedGetRelatedEquipmentsQueryResult => _fullyPopulatedGetRelatedEquipmentsQueryResult;

        public SerializationTestDataFixture()
        {
        }

        /// <summary>
        /// Create a fully populated GetRelatedEquipmentsQueryResult to be used for testing serialization
        /// </summary>
        private static GetRelatedEquipmentQueryResult CreateFullyPopulatedGetRelatedEquipmentsQueryResult
        {
            get
            {
                var queriedRouteNetworkElement = new RouteNetworkElement() { Name = "Hest" };

                var queryResult = new GetRelatedEquipmentQueryResult(queriedRouteNetworkElement);

                SpanEquipmentSpecification spec = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 10x10", new SpanStructureTemplate(Guid.NewGuid(), 1, 1, Array.Empty<SpanStructureTemplate>()));

                queryResult.SpanEquipmentSpecifications = new SpanEquipmentSpecification[] { spec };

                RelatedSpanStructure rootStructure = new RelatedSpanStructure(Guid.NewGuid(), spec.Id, Array.Empty<SpanSegment>());

                queryResult.RelatedSpanEquipment = new RelatedSpanEquipment[]
                {
                        new RelatedSpanEquipment(Guid.NewGuid(), spec.Id, rootStructure)
                        {
                            Name = "My Span Equipment"
                        }
                };

                return queryResult;
            }
        }

        public void Dispose()
        {
        }
    }
}
