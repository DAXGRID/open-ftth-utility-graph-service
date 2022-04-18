using DAX.ObjectVersioning.Core;
using DAX.ObjectVersioning.Graph;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenFTTH.UtilityGraphService.Business.Graph.Projections
{
    /// <summary>
    /// Write up internal connectivity in terminal equipments - i.e. in splitters and wdm modules
    /// </summary>
    public static class UtilityGraphTerminalEquipmentProjections
    {
        public static void ApplyInternalConnectivityToGraph(NodeContainer nodeContainer, TerminalEquipment terminalEquipment, UtilityGraph graph)
        {
            var trans = graph.CreateTransaction();

            Dictionary<Guid, UtilityGraphInternalEquipmentConnectivityNode> connectivityNodeCreated = new();

            for (int structureIndex = 0; structureIndex < terminalEquipment.TerminalStructures.Length; structureIndex++)
            {
                var terminalStructure = terminalEquipment.TerminalStructures[structureIndex];

                foreach (var terminal in terminalStructure.Terminals)
                {
                    if (terminal.InternalConnectivityNodeId != null)
                    {
                        Guid internalConnectivityNodeId = terminal.InternalConnectivityNodeId.Value;

                        // Create connectivity node if not already created
                        if (!connectivityNodeCreated.ContainsKey(internalConnectivityNodeId))
                        {
                            var newInternalConnectivityNode = new UtilityGraphInternalEquipmentConnectivityNode(internalConnectivityNodeId, terminalEquipment.Id, nodeContainer.RouteNodeId, (ushort)structureIndex);

                            connectivityNodeCreated[internalConnectivityNodeId] = newInternalConnectivityNode;

                            trans.Add(newInternalConnectivityNode);
                        }

                        // Add terminal to graph
                        var connectedTerminal = CreateTerminal(terminal.Id, nodeContainer.RouteNodeId, graph, trans);

                        // Connect terminal with internal connectivity node
                        if (terminal.Direction == TerminalDirectionEnum.IN)
                        {
                            var internalConnectivityLink = new UtilityGraphInternalEquipmentConnectivityLink(Guid.NewGuid(), connectedTerminal, connectivityNodeCreated[internalConnectivityNodeId], terminalEquipment.Id, nodeContainer.RouteNodeId, (ushort)structureIndex);
                            trans.Add(internalConnectivityLink);
                        }
                        else
                        {
                            var internalConnectivityLink = new UtilityGraphInternalEquipmentConnectivityLink(Guid.NewGuid(), connectivityNodeCreated[internalConnectivityNodeId], connectedTerminal, terminalEquipment.Id, nodeContainer.RouteNodeId, (ushort)structureIndex);
                            trans.Add(internalConnectivityLink);
                        }
                    }
                }
            }
        
            trans.Commit();
        }

        private static UtilityGraphConnectedTerminal CreateTerminal(Guid terminalId, Guid terminalNodeOfInterestId, UtilityGraph graph, ITransaction transaction)
        {
            // Try find terminal in graph
            var terminalRef = graph.GetTerminal(terminalId, transaction.Version.InternalVersionId);

            // Try find in transaction
            if (terminalRef == null)
            {
                terminalRef = transaction.GetObject(terminalId) as IUtilityGraphTerminalRef;
            }

            if (terminalRef == null)
            {
                if (graph.TryGetGraphElement<IUtilityGraphTerminalRef>(terminalId, out var utilityGraphTerminalRef))
                {
                    var terminal = new UtilityGraphConnectedTerminal(terminalId, utilityGraphTerminalRef.TerminalEquipmentId, terminalNodeOfInterestId, utilityGraphTerminalRef.StructureIndex, utilityGraphTerminalRef.TerminalIndex);
                    transaction.Add(terminal);
                    graph.UpdateIndex(terminalId, terminal);

                    return terminal;
                }
                else
                {
                    var terminal = new UtilityGraphConnectedTerminal(terminalId, Guid.Empty, terminalNodeOfInterestId);
                    transaction.Add(terminal);

                    return terminal;
                }
            }
            else
            {
                return (UtilityGraphConnectedTerminal)terminalRef;
            }
        }

    }
}
