using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Tests.Fixtures
{

    public class SerializationTestDataFixture : IDisposable
    {
        GetRelatedEquipmentsQueryResult _fullyPopulatedGetRelatedEquipmentsQueryResult = CreateFullyPopulatedGetRelatedEquipmentsQueryResult;

        public GetRelatedEquipmentsQueryResult FullyPopulatedGetRelatedEquipmentsQueryResult => _fullyPopulatedGetRelatedEquipmentsQueryResult;

        public SerializationTestDataFixture()
        {
        }

        /// <summary>
        /// Create a fully populated GetRelatedEquipmentsQueryResult to be used for testing serialization
        /// </summary>
        private static GetRelatedEquipmentsQueryResult CreateFullyPopulatedGetRelatedEquipmentsQueryResult
        {
            get
            {
                var queriedRouteNetworkElement = new RouteNetworkElement() { Name = "Hest" };

                var queryResult = new GetRelatedEquipmentsQueryResult(queriedRouteNetworkElement);

                SpanEquipmentSpecification spec = new SpanEquipmentSpecification(Guid.NewGuid(), "Conduit", "Ø50 10x10", "0.0.1");

                queryResult.SpanEquipmentSpecifications = new SpanEquipmentSpecification[] { spec };

                RelatedSpanStructure rootStructure = new RelatedSpanStructure(Guid.NewGuid(), spec.Id)
                {
                    Name = "My Span Structure"
                };

                queryResult.RelatedSpanEquipments = new RelatedSpanEquipment[]
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
