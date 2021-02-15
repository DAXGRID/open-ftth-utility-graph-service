﻿using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Util;
using OpenFTTH.UtilityGraphService.Business.SpanEquipment.Events;
using System;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipment
{
    /// <summary>
    /// Aggregate holding span structure specifications
    /// </summary>
    public class SpanStructureSpecificationsAR : AggregateBase
    {
        public static readonly Guid UUID = Guid.Parse("a5f00c41-a642-4d3e-be10-907c6a727a6a");

        private LookupCollection<SpanStructureSpecification> _spanStructureSpecifications = new LookupCollection<SpanStructureSpecification>();

        public SpanStructureSpecificationsAR()
        {
            Id = UUID;
            Register<SpanStructureSpecificationAdded>(Apply);
            Register<SpanStructureSpecificationDeprecated>(Apply);
        }

        private void Apply(SpanStructureSpecificationDeprecated obj)
        {
            _spanStructureSpecifications[obj.SpanStructureSpecificationId] = _spanStructureSpecifications[obj.SpanStructureSpecificationId] with { Deprecated = true };
        }

        private void Apply(SpanStructureSpecificationAdded @event)
        {
            _spanStructureSpecifications.Add(@event.Specification);
        }

        public void AddSpecification(SpanStructureSpecification spanStructureSpecification)
        {
            if (_spanStructureSpecifications.ContainsKey(spanStructureSpecification.Id))
                throw new ArgumentException($"A span structure specification with id: {spanStructureSpecification.Id} already exists.");

            RaiseEvent(new SpanStructureSpecificationAdded(spanStructureSpecification));
        }

        public void DeprecatedSpecification(Guid specificationId)
        {
            if (!_spanStructureSpecifications.ContainsKey(specificationId))
                throw new ArgumentException($"Cannot find span structure specification with id: {specificationId}");

            RaiseEvent(new SpanStructureSpecificationDeprecated(specificationId));
        }
    }
}
