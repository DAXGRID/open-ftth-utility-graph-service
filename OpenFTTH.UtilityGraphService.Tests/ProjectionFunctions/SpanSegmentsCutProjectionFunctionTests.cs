using FluentAssertions;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.Graph.Projections;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using Xunit;

namespace OpenFTTH.UtilityGraphService.Tests.ProjectionFunctions
{
    public class SpanSegmentsCutProjectionFunctionTests
    {
        [Fact]
        public void TestCutStructureOneTime_ShouldSucceed()
        {
            // Setup
            var spanSegmentIdToCut = Guid.NewGuid();
            var newSegmentId1 = Guid.NewGuid();
            var newSegmentId2 = Guid.NewGuid();

            var existingSpanEquipment = new OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.SpanEquipment(
                id: Guid.NewGuid(),
                specificationId: Guid.NewGuid(),
                walkOfInterestId: Guid.NewGuid(),
                nodesOfInterestIds: new Guid[] { Guid.NewGuid(), Guid.NewGuid() },
                spanStructures: 
                    new SpanStructure[]
                    {
                        new SpanStructure(
                            id: Guid.NewGuid(),
                            specificationId: Guid.NewGuid(),
                            level: 1,
                            position: 1,
                            parentPosition: 0,
                            spanSegments:
                                new SpanSegment[]
                                {
                                    new SpanSegment(spanSegmentIdToCut, 0, 1)
                                }
                        )
                    }
            );

            var cutEvent = new SpanSegmentsCut(
                spanEquipmentId: existingSpanEquipment.Id,
                cutNodeOfInterestId: Guid.NewGuid(),
                cuts:
                    new SpanSegmentCutInfo[]
                    {
                        new SpanSegmentCutInfo(
                            oldSpanSegmentId: spanSegmentIdToCut,
                            oldStructureIndex: 0,
                            oldSegmentIndex: 0,
                            newSpanSegmentId1: newSegmentId1,
                            newSpanSegmentId2: newSegmentId2
                            )
                    }
             );


            var newSpanEquipment = SpanEquipmentProjectionFunctions.Apply(existingSpanEquipment, cutEvent);

            newSpanEquipment.NodesOfInterestIds.Length.Should().Be(3);
            newSpanEquipment.SpanStructures[0].SpanSegments.Length.Should().Be(2);

            newSpanEquipment.SpanStructures[0].SpanSegments[0].Id.Should().Be(newSegmentId1);
            newSpanEquipment.SpanStructures[0].SpanSegments[0].FromNodeOfInterestIndex.Should().Be(0);
            newSpanEquipment.SpanStructures[0].SpanSegments[0].ToNodeOfInterestIndex.Should().Be(2);

            newSpanEquipment.SpanStructures[0].SpanSegments[1].Id.Should().Be(newSegmentId2);
            newSpanEquipment.SpanStructures[0].SpanSegments[1].FromNodeOfInterestIndex.Should().Be(2);
            newSpanEquipment.SpanStructures[0].SpanSegments[1].ToNodeOfInterestIndex.Should().Be(1);
        }


        [Fact]
        public void TestCutSameStructureMultipleTime_ShouldSucceed()
        {
            // Setup
            var cut1spanSegmentId = Guid.NewGuid();
            var cut1newSegmentId1 = Guid.NewGuid();
            var cut1newSegmentId2 = Guid.NewGuid();

            var existingSpanEquipment = new OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.SpanEquipment(
                id: Guid.NewGuid(),
                specificationId: Guid.NewGuid(),
                walkOfInterestId: Guid.NewGuid(),
                nodesOfInterestIds: new Guid[] { Guid.NewGuid(), Guid.NewGuid() },
                spanStructures:
                    new SpanStructure[]
                    {
                        new SpanStructure(
                            id: Guid.NewGuid(),
                            specificationId: Guid.NewGuid(),
                            level: 1,
                            position: 1,
                            parentPosition: 0,
                            spanSegments:
                                new SpanSegment[]
                                {
                                    new SpanSegment(cut1spanSegmentId, 0, 1)
                                }
                        )
                    }
            );

            var cut1Event = new SpanSegmentsCut(
                spanEquipmentId: existingSpanEquipment.Id,
                cutNodeOfInterestId: Guid.NewGuid(),
                cuts:
                    new SpanSegmentCutInfo[]
                    {
                        new SpanSegmentCutInfo(
                            oldSpanSegmentId: cut1spanSegmentId,
                            oldStructureIndex: 0,
                            oldSegmentIndex: 0,
                            newSpanSegmentId1: cut1newSegmentId1,
                            newSpanSegmentId2: cut1newSegmentId2
                            )
                    }
             );

            var cut2newSegmentId1 = Guid.NewGuid();
            var cut2newSegmentId2 = Guid.NewGuid();

            var cut2Event = new SpanSegmentsCut(
                 spanEquipmentId: existingSpanEquipment.Id,
                 cutNodeOfInterestId: Guid.NewGuid(),
                 cuts:
                     new SpanSegmentCutInfo[]
                     {
                                    new SpanSegmentCutInfo(
                                        oldSpanSegmentId: cut1newSegmentId1,
                                        oldStructureIndex: 0,
                                        oldSegmentIndex: 0,
                                        newSpanSegmentId1: cut2newSegmentId1,
                                        newSpanSegmentId2: cut2newSegmentId2
                                        )
                     }
              );

            // Act
            var newSpanEquipment = SpanEquipmentProjectionFunctions.Apply(existingSpanEquipment, cut1Event);
            newSpanEquipment = SpanEquipmentProjectionFunctions.Apply(newSpanEquipment, cut2Event);


            // Assert
            newSpanEquipment.NodesOfInterestIds.Length.Should().Be(4);
            newSpanEquipment.SpanStructures[0].SpanSegments.Length.Should().Be(3);

            newSpanEquipment.SpanStructures[0].SpanSegments[0].Id.Should().Be(cut2newSegmentId1);
            newSpanEquipment.SpanStructures[0].SpanSegments[0].FromNodeOfInterestIndex.Should().Be(0);
            newSpanEquipment.SpanStructures[0].SpanSegments[0].ToNodeOfInterestIndex.Should().Be(3);

            newSpanEquipment.SpanStructures[0].SpanSegments[1].Id.Should().Be(cut2newSegmentId2);
            newSpanEquipment.SpanStructures[0].SpanSegments[1].FromNodeOfInterestIndex.Should().Be(3);
            newSpanEquipment.SpanStructures[0].SpanSegments[1].ToNodeOfInterestIndex.Should().Be(2);

            newSpanEquipment.SpanStructures[0].SpanSegments[2].Id.Should().Be(cut1newSegmentId2);
            newSpanEquipment.SpanStructures[0].SpanSegments[2].FromNodeOfInterestIndex.Should().Be(2);
            newSpanEquipment.SpanStructures[0].SpanSegments[2].ToNodeOfInterestIndex.Should().Be(1);
        }



    }
}
