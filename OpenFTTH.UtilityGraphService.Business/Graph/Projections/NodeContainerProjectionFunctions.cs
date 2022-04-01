using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.Business.NodeContainers.Events;
using OpenFTTH.UtilityGraphService.Business.SpanEquipments.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Projections
{
    public static class NodeContainerProjectionFunctions
    {
        public static NodeContainer Apply(NodeContainer existingSpanEquipment, NodeContainerVerticalAlignmentReversed alignmentReversed)
        {
            var newAllignment = existingSpanEquipment.VertialContentAlignmemt == NodeContainerVerticalContentAlignmentEnum.Bottom ? NodeContainerVerticalContentAlignmentEnum.Top : NodeContainerVerticalContentAlignmentEnum.Bottom;

            return existingSpanEquipment with
            {
                VertialContentAlignmemt = newAllignment
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerManufacturerChanged @event)
        {
            return existingEquipment with
            {
                ManufacturerId = @event.ManufacturerId
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerSpecificationChanged @event)
        {
            return existingEquipment with
            {
                SpecificationId = @event.NewSpecificationId,
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerRackAdded @event)
        {
            List<Rack> newRackList = new();

            if (existingEquipment.Racks != null)
                newRackList.AddRange(existingEquipment.Racks);

            newRackList.Add(new Rack(@event.RackId, @event.RackName, @event.RackPosition, @event.RackSpecificationId, @event.RackHeightInUnits, new SubrackMount[] { }));

            return existingEquipment with
            {
                Racks = newRackList.ToArray()
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerTerminalEquipmentAdded @event)
        {
            List<Guid> newTerminalEquipmentRefList = new();

            if (existingEquipment.TerminalEquipmentReferences != null)
                newTerminalEquipmentRefList.AddRange(existingEquipment.TerminalEquipmentReferences);

            newTerminalEquipmentRefList.Add(@event.TerminalEquipmentId);

            return existingEquipment with
            {
                TerminalEquipmentReferences = newTerminalEquipmentRefList.ToArray()
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerTerminalEquipmentsAddedToRack @event)
        {
            if (existingEquipment.Racks == null)
                return existingEquipment;

            Rack? rack = existingEquipment.Racks.FirstOrDefault(r => r.Id == @event.RackId);

            if (rack == null)
                return existingEquipment;

            int totalHeight = @event.TerminalEquipmentHeightInUnits * @event.TerminalEquipmentIds.Count();

            List<SubrackMount> keepList = new();
            List<SubrackMount> moveUpList = new();

            bool foundFirstEquipmentWithinBlock = false;
            int moveUpUnits = 0;

            foreach (var existingSubrackMount in rack.SubrackMounts.OrderBy(s => s.Position))
            {
                // Check if existing mount found within new equipment(s) block
                if (!foundFirstEquipmentWithinBlock && existingSubrackMount.Position >= @event.StartUnitPosition && existingSubrackMount.Position < (@event.StartUnitPosition + totalHeight))
                {
                    foundFirstEquipmentWithinBlock = true;
                    moveUpUnits = totalHeight - (existingSubrackMount.Position - @event.StartUnitPosition);
                }

                if (foundFirstEquipmentWithinBlock)
                {
                    // We're going to move it up
                    moveUpList.Add(existingSubrackMount with { Position = existingSubrackMount.Position + moveUpUnits });
                }
                else
                {
                    // We keep the position
                    keepList.Add(existingSubrackMount);
                }
            }

            // Add the new terminal equipments to rack
            int insertPosition = @event.StartUnitPosition;

            foreach (var equipmentId in @event.TerminalEquipmentIds)
            {
                keepList.Add(new SubrackMount(equipmentId, insertPosition, @event.TerminalEquipmentHeightInUnits));
                insertPosition += @event.TerminalEquipmentHeightInUnits;
            }

            // Add the moved up terminal equipments
            keepList.AddRange(moveUpList);

            Rack[] newRacks = new Rack[existingEquipment.Racks.Length];

            existingEquipment.Racks.CopyTo(newRacks, 0);

            newRacks[Array.IndexOf(existingEquipment.Racks, rack)] = new Rack(rack.Id, rack.Name, rack.Position, rack.SpecificationId, rack.HeightInUnits, keepList.ToArray());

            return existingEquipment with
            {
                Racks = newRacks
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerRackSpecificationChanged @event)
        {
            if (existingEquipment.Racks == null)
                return existingEquipment;

            List<Rack> newRackList = new List<Rack>();

            foreach (var rack in existingEquipment.Racks)
            {
                if (rack.Id == @event.RackId)
                {
                    newRackList.Add(rack with { SpecificationId = @event.NewSpecificationId });
                }
                else
                {
                    newRackList.Add(rack);
                }
            }

            return existingEquipment with
            {
                Racks = newRackList.ToArray()
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerRackNameChanged @event)
        {
            if (existingEquipment.Racks == null)
                return existingEquipment;

            List<Rack> newRackList = new List<Rack>();

            foreach (var rack in existingEquipment.Racks)
            {
                if (rack.Id == @event.RackId)
                {
                    newRackList.Add(rack with { Name = @event.NewName });
                }
                else
                {
                    newRackList.Add(rack);
                }
            }

            return existingEquipment with
            {
                Racks = newRackList.ToArray()
            };
        }

        public static NodeContainer Apply(NodeContainer existingEquipment, NodeContainerRackHeightInUnitsChanged @event)
        {
            if (existingEquipment.Racks == null)
                return existingEquipment;

            List<Rack> newRackList = new List<Rack>();

            foreach (var rack in existingEquipment.Racks)
            {
                if (rack.Id == @event.RackId)
                {
                    newRackList.Add(rack with { HeightInUnits = @event.NewHeightInUnits });
                }
                else
                {
                    newRackList.Add(rack);
                }
            }

            return existingEquipment with
            {
                Racks = newRackList.ToArray()
            };
        }


    }
}
