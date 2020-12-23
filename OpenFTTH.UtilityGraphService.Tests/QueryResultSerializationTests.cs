using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Model.RouteNetwork;
using OpenFTTH.UtilityGraphService.API.Queries;
using System;
using Xunit;
using FluentAssertions;
using OpenFTTH.UtilityGraphService.Tests.Fixtures;

namespace OpenFTTH.UtilityGraphService.Business.Tests
{
    public class QueryResultSerializationTests : IClassFixture<SerializationTestDataFixture>
    {
        private readonly SerializationTestDataFixture TestData;

        public QueryResultSerializationTests(SerializationTestDataFixture testData)
        {
            this.TestData = testData;
        }

        [Fact]
        public void NewtonsoftJsonSerializationTest()
        {
            var json = JsonConvert.SerializeObject(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);

            GetRelatedEquipmentsQueryResult queryResultDeserialized = JsonConvert.DeserializeObject<GetRelatedEquipmentsQueryResult>(json);

            queryResultDeserialized.Should().BeEquivalentTo(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);
        }

        [Fact]
        public void SystemTextJsonSerializationTest()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);

            GetRelatedEquipmentsQueryResult? queryResultDeserialized = System.Text.Json.JsonSerializer.Deserialize<GetRelatedEquipmentsQueryResult>(json);

            queryResultDeserialized.Should().BeEquivalentTo(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);
        }


    }
}
