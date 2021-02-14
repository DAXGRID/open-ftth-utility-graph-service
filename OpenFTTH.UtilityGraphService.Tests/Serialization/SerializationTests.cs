using FluentAssertions;
using Newtonsoft.Json;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Tests.Fixtures;
using Xunit;

namespace OpenFTTH.UtilityGraphService.Business.Tests
{
    public class SerializationTests : IClassFixture<SerializationTestDataFixture>
    {
        private readonly SerializationTestDataFixture TestData;

        public SerializationTests(SerializationTestDataFixture testData)
        {
            this.TestData = testData;
        }

        [Fact]
        public void NewtonsoftJsonSerializationTest()
        {
            //var json = JsonConvert.SerializeObject(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);

            //GetRelatedEquipmentQueryResult queryResultDeserialized = JsonConvert.DeserializeObject<GetRelatedEquipmentQueryResult>(json);

            ///queryResultDeserialized.Should().BeEquivalentTo(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);
        }

        [Fact]
        public void SystemTextJsonSerializationTest()
        {
            ///var json = System.Text.Json.JsonSerializer.Serialize(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);

            ///GetRelatedEquipmentQueryResult? queryResultDeserialized = System.Text.Json.JsonSerializer.Deserialize<GetRelatedEquipmentQueryResult>(json);

            ///queryResultDeserialized.Should().BeEquivalentTo(TestData.FullyPopulatedGetRelatedEquipmentsQueryResult);
        }
    }
}
