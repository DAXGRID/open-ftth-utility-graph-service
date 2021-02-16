using Newtonsoft.Json;
using OpenFTTH.UtilityGraphService.API.Model;
using OpenFTTH.UtilityGraphService.API.Util;
using System;
using Xunit;
using FluentAssertions;

namespace OpenFTTH.UtilityGraphService.Tests.LookupCollection
{
    public class LookupCollectionTests
    {
        [Fact]
        public void NewtonsoftJsonSerializationTest()
        {
            var foo1 = new Foo() { Id = Guid.NewGuid(), Name = "Hest" };
            var foo2 = new Foo() { Id = Guid.NewGuid(), Name = "Hund" };

            var lookupCollection = new LookupCollection<Foo>(new[] { foo1, foo2 });

            var json = JsonConvert.SerializeObject(lookupCollection);

            var deserializedObject = JsonConvert.DeserializeObject<LookupCollection<Foo>>(json);

            deserializedObject[foo1.Id].Name.Should().Be("Hest");
            deserializedObject[foo2.Id].Name.Should().Be("Hund");
            deserializedObject.TryGetValue(foo1.Id, out var _).Should().BeTrue();
            deserializedObject.TryGetValue(Guid.NewGuid(), out var _).Should().BeFalse();
        }

        private class Foo : IIdentifiedObject
        {
            public Guid Id { get; set; }

            public string? Name { get; set; }

            public string? Description { get; init; }
        }


    }
}
