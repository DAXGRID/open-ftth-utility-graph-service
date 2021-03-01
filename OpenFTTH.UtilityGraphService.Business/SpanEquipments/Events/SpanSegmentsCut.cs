﻿using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events
{
    public record SpanSegmentsCut
    {
        public Guid SpanEquipmentId { get; }
        public Guid CutNodeOfInterestId { get; }
        public SpanSegmentCutInfo[] Cuts { get; }

        public SpanSegmentsCut(Guid spanEquipmentId, Guid cutNodeOfInterestId, SpanSegmentCutInfo[] cuts)
        {
            SpanEquipmentId = spanEquipmentId;
            CutNodeOfInterestId = cutNodeOfInterestId;
            Cuts = cuts;
        }
    }

    public record SpanSegmentCutInfo
    {
        public Guid OldSpanSegmentId { get; }
        public UInt16 OldStructureIndex { get; }
        public UInt16 OldSegmentIndex { get; }
        public Guid NewSpanSegmentId1 { get; }
        public Guid NewSpanSegmentId2 { get; }

        public SpanSegmentCutInfo(Guid oldSpanSegmentId, ushort oldStructureIndex, ushort oldSegmentIndex, Guid newSpanSegmentId1, Guid newSpanSegmentId2)
        {
            OldSpanSegmentId = oldSpanSegmentId;
            OldStructureIndex = oldStructureIndex;
            OldSegmentIndex = oldSegmentIndex;
            NewSpanSegmentId1 = newSpanSegmentId1;
            NewSpanSegmentId2 = newSpanSegmentId2;
        }
    }
}
