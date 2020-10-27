using Microsoft.Extensions.Logging;
using OpenFTTH.Events.RouteNetwork;
using OpenFTTH.UtilityGraphService.Business.Node;
using OpenFTTH.UtilityGraphService.Query.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OpenFTTH.UtilityGraphService.Business.Tests
{
    public class NodeEquipmentTests
    {
        [Fact]
        public void CreateNodeEquipment_InRouteNodeThatDontExists_ShouldThrowArgumentException()
        {
            ILoggerFactory loggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();

            var queryApi = new InMemoryQueryHandler(loggerFactory);

            Assert.Throws<ArgumentException>(() => new NodeEquipment(queryApi, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        }
    }
}
