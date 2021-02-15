using System;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork
{
    public class SpanStructureTemplate
    {
        public Guid SpanStructureSpecificationId { get; }
        public int Level { get; }
        public int Position { get; }
        public SpanStructureTemplate[] ChildTemplates { get; }

        public SpanStructureTemplate(Guid spanStructureSpecificationId, int level, int position, SpanStructureTemplate[] childTemplates)
        {
            SpanStructureSpecificationId = spanStructureSpecificationId;
            Level = level;
            Position = position;
            ChildTemplates = childTemplates;
        }

        public List<SpanStructureTemplate> GetAllSpanStructureTemplatesRecursive()
        {
            List<SpanStructureTemplate> result = new List<SpanStructureTemplate>();

            GetAllSpanStructureTemplatesRecursiveInternal(result);

            return result;
        }

        private void GetAllSpanStructureTemplatesRecursiveInternal(List<SpanStructureTemplate> result)
        {
            result.Add(this);

            foreach (var childTemplate in ChildTemplates)
            {
                childTemplate.GetAllSpanStructureTemplatesRecursiveInternal(result);
            }
        }
    }
}
