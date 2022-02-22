using DAX.ObjectVersioning.Graph;
using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.RouteNetwork.API.Model;
using OpenFTTH.RouteNetwork.API.Queries;
using OpenFTTH.Util;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using OpenFTTH.UtilityGraphService.Business.TerminalEquipments.Projections;
using OpenFTTH.UtilityGraphService.Business.Trace.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.Trace.QueryHandling
{
    public class GetConnectivityTraceQueryHandler
        : IQueryHandler<GetConnectivityTraceView, Result<ConnectivityTraceView>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;
        private LookupCollection<TerminalEquipmentSpecification> _terminalEquipmentSpecifications;


        public GetConnectivityTraceQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
            _terminalEquipmentSpecifications = _eventStore.Projections.Get<TerminalEquipmentSpecificationsProjection>().Specifications;
        }

        public Task<Result<ConnectivityTraceView>> HandleAsync(GetConnectivityTraceView query)
        {
            if (_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphTerminalRef>(query.TerminalOrSpanSegmentId, out var utilityGraphTerminalRef))
            {
                return Task.FromResult(BuildTraceViewFromTerminal(utilityGraphTerminalRef));
            }
            else if (_utilityNetwork.Graph.TryGetGraphElement<IUtilityGraphSegmentRef>(query.TerminalOrSpanSegmentId, out var utilityGraphSegmentRef))
            {
                return Task.FromResult(BuildTraceViewFromSegment(query.TerminalOrSpanSegmentId, utilityGraphSegmentRef));
            }

            return Task.FromResult(NotConnected());
        }

        private Result<ConnectivityTraceView> BuildTraceViewFromTerminal(IUtilityGraphTerminalRef sourceTerminalRef)
        {
            var traceResult = _utilityNetwork.Graph.Trace(sourceTerminalRef.TerminalId);

            List<IGraphObject> traceElements = new();

            if (traceResult.Upstream.Length > 0)
                traceElements.AddRange(traceResult.Upstream.Reverse());
            if (traceResult.Source != null)
                traceElements.Add(traceResult.Source);
            if (traceResult.Downstream.Length > 0)
                traceElements.AddRange(traceResult.Downstream);


            var relatedData = new RouteNetworkDataHolder(_eventStore, _utilityNetwork, _queryDispatcher, traceElements.OfType<IUtilityGraphTerminalRef>().Select(t => t.RouteNodeId).ToArray());

            var terminalEquipment = sourceTerminalRef.TerminalEquipment(_utilityNetwork);

            ReverseIfNeeded(traceElements, relatedData.RouteNetworkElements);

            List<ConnectivityTraceViewHopInfo> hops = new();

            int hopSeqNo = 1;

            for (int graphElementIndex = 0; graphElementIndex < traceElements.Count; graphElementIndex++)
            {
                var currentGraphElement = traceElements[graphElementIndex];

                if (currentGraphElement is IUtilityGraphTerminalRef terminalRef)
                {
                    string connectionCableInfo = "";

                    if (graphElementIndex < (traceElements.Count - 1))
                    {
                        connectionCableInfo = GetConnectionInfo(relatedData, traceElements[graphElementIndex + 1] as IUtilityGraphSegmentRef);
                    }

                    hops.Add(
                        new ConnectivityTraceViewHopInfo(
                            hopSeqNo,
                            level: 0,
                            isSplitter: false,
                            isTraceSource: false,
                            node: relatedData.GetNodeName(terminalRef.RouteNodeId),
                            equipment: relatedData.GetCompactEquipmentWithTypeInfoString(terminalRef.RouteNodeId, terminalEquipment),
                            terminalStructure: relatedData.GetEquipmentStructureInfoString(terminalRef),
                            terminal: relatedData.GetEquipmentTerminalInfoString(terminalRef),
                            connectionInfo: connectionCableInfo,
                            totalLength: 2,
                            routeSegmentGeometries: Array.Empty<string>(),
                            routeSegmentIds: Array.Empty<Guid>()
                        )
                    );

                    hopSeqNo++;
                }
            }

            return Result.Ok(new ConnectivityTraceView("FK000000", hops.ToArray()));

        }

        private Result<ConnectivityTraceView> BuildTraceViewFromSegment(Guid spanSegmentId, IUtilityGraphSegmentRef utilityGraphSegmentRef)
        {
            var traceResult = _utilityNetwork.Graph.Trace(spanSegmentId);

            List<IGraphObject> traceElements = traceResult.All;

            var relatedData = new RouteNetworkDataHolder(_eventStore, _utilityNetwork, _queryDispatcher, traceElements.OfType<IUtilityGraphTerminalRef>().Select(t => t.RouteNodeId).ToArray());

            ReverseIfNeeded(traceElements, relatedData.RouteNetworkElements);

            List<ConnectivityTraceViewHopInfo> hops = new();

            int hopSeqNo = 1;

            for (int graphElementIndex = 0; graphElementIndex < traceElements.Count; graphElementIndex++)
            {
                var currentGraphElement = traceElements[graphElementIndex];

                if (currentGraphElement is IUtilityGraphTerminalRef terminalRef)
                {
                    string connectionCableInfo = "";

                    if (graphElementIndex < (traceElements.Count - 1))
                    {
                        connectionCableInfo = GetConnectionInfo(relatedData, traceElements[graphElementIndex + 1] as IUtilityGraphSegmentRef);
                    }

                    hops.Add(
                        new ConnectivityTraceViewHopInfo(
                            hopSeqNo,
                            level: 0,
                            isSplitter: false,
                            isTraceSource: false,
                            node: relatedData.GetNodeName(terminalRef.RouteNodeId),
                            equipment: relatedData.GetCompactEquipmentWithTypeInfoString(terminalRef.RouteNodeId, terminalRef.TerminalEquipment(_utilityNetwork)),
                            terminalStructure: relatedData.GetEquipmentStructureInfoString(terminalRef),
                            terminal: relatedData.GetEquipmentTerminalInfoString(terminalRef),
                            connectionInfo: connectionCableInfo,
                            totalLength: 2,
                            routeSegmentGeometries: Array.Empty<string>(),
                            routeSegmentIds: Array.Empty<Guid>()
                        )
                    );

                    hopSeqNo++;
                }
            }

            return Result.Ok(new ConnectivityTraceView("FK000000", hops.ToArray()));

        }


        private string GetConnectionInfo(RouteNetworkDataHolder relatedData, IUtilityGraphSegmentRef? utilityGraphSegmentRef)
        {
            var spanEquipment = utilityGraphSegmentRef.SpanEquipment(_utilityNetwork);

            var nFibers = spanEquipment.SpanStructures.Count() - 1;

            return $"{spanEquipment.Name} ({nFibers}) Fiber {utilityGraphSegmentRef.StructureIndex}";
        }

      

        private List<IGraphObject> ReverseIfNeeded(List<IGraphObject> trace, LookupCollection<RouteNetworkElement> routeNetworkElements)
        {
            var terminals = trace.OfType<IUtilityGraphTerminalRef>();

            if (terminals.Count() > 1)
            {
                var currentFromNode = routeNetworkElements[terminals.First().RouteNodeId];
                var currentToNode = routeNetworkElements[terminals.Last().RouteNodeId];

                if (currentFromNode != null && currentFromNode.RouteNodeInfo != null && currentFromNode.RouteNodeInfo.Function != null)
                {
                    var currentFromNodeRank = (int)currentFromNode.RouteNodeInfo.Function;

                    if (currentToNode != null && currentToNode.RouteNodeInfo != null && currentToNode.RouteNodeInfo.Function != null)
                    {
                        var currentToNodeRank = (int)currentToNode.RouteNodeInfo.Function;

                        if (currentToNodeRank < currentFromNodeRank)
                        {
                            trace.Reverse();
                            return trace;
                        }
                    }
                }
            }

            return trace;
        }


        private Result<ConnectivityTraceView> NotConnected()
        {
            return Result.Ok(new ConnectivityTraceView("Not connected", new ConnectivityTraceViewHopInfo[] { }));
        }

      


        private ConnectivityTraceView BuildTestData()
        {
            List<ConnectivityTraceViewHopInfo> hops = new();

            hops.Add(
                new ConnectivityTraceViewHopInfo(
                    1,
                    level: 0,
                    isSplitter: false,
                    isTraceSource: false,
                    node: "GALARH",
                    equipment: "RACK 3 - OLT 1",
                    terminalStructure: "Kort 1",
                    terminal: "Port 1",
                    connectionInfo: "Intern forb",
                    totalLength: 2,
                    routeSegmentGeometries: Array.Empty<string>(),
                    routeSegmentIds: Array.Empty<Guid>()
                )
            );

            hops.Add(
               new ConnectivityTraceViewHopInfo(
                   2,
                   level: 0,
                   isSplitter: false,
                   isTraceSource: false,
                   node: "GALARH",
                   equipment: "RACK 2 - WDM Type 2",
                   terminalStructure: "Slot 1",
                   terminal: "IP AB / COM A",
                   connectionInfo: "Intern forb",
                   totalLength: 4,
                   routeSegmentGeometries: Array.Empty<string>(),
                   routeSegmentIds: Array.Empty<Guid>()
               )
           );

           hops.Add(
              new ConnectivityTraceViewHopInfo(
                  3,
                  level: 0,
                  isSplitter: false,
                  isTraceSource: true,
                  node: "GALARH",
                  equipment: "RACK 1 - GPS 1",
                  terminalStructure: "Bakke 1",
                  terminal: "Søm 1",
                  connectionInfo: "K12345678 (72) Fiber 1",
                  totalLength: 480,
                  routeSegmentGeometries: Array.Empty<string>(),
                  routeSegmentIds: Array.Empty<Guid>()
              )
            );

            hops.Add(
              new ConnectivityTraceViewHopInfo(
                  4,
                  level: 0,
                  isSplitter: false,
                  isTraceSource: false,
                  node: "F1200",
                  equipment: "RACK 1 - GPS 1",
                  terminalStructure: "Bakke 2",
                  terminal: "Søm 1",
                  connectionInfo: "Intern forb",
                  totalLength: 482,
                  routeSegmentGeometries: Array.Empty<string>(),
                  routeSegmentIds: Array.Empty<Guid>()
              )
            );


            // splitter ud 1

            hops.Add(
              new ConnectivityTraceViewHopInfo(
                  5,
                  level: 0,
                  isSplitter: true,
                  isTraceSource: false,
                  node: "F1200",
                  equipment: "RACK 1 - 1:32 Splitter",
                  terminalStructure: "Splitter 1",
                  terminal: "IND 1 / UD 1",
                  connectionInfo: "Intern forb",
                  totalLength: 484,
                  routeSegmentGeometries: Array.Empty<string>(),
                  routeSegmentIds: Array.Empty<Guid>()
              )
            );

            hops.Add(
             new ConnectivityTraceViewHopInfo(
                 6,
                 level: 1,
                 isSplitter: false,
                 isTraceSource: false,
                 node: "F1200",
                 equipment: "RACK 1 - GPS 2",
                 terminalStructure: "Bakke 1",
                 terminal: "Søm 11",
                 connectionInfo: "K12434434 (48) Fiber 10",
                 totalLength: 1250,
                 routeSegmentGeometries: Array.Empty<string>(),
                 routeSegmentIds: Array.Empty<Guid>()
             )
           );

           hops.Add(
             new ConnectivityTraceViewHopInfo(
                 7,
                 level: 1,
                 isSplitter: false,
                 isTraceSource: false,
                 node: "F1230",
                 equipment: "BUDI 2s",
                 terminalStructure: "Bakke 1",
                 terminal: "Søm 11",
                 connectionInfo: "K12353434 (2) Fiber 1",
                 totalLength: 1434,
                 routeSegmentGeometries: Array.Empty<string>(),
                 routeSegmentIds: Array.Empty<Guid>()
             )
           );

            hops.Add(
             new ConnectivityTraceViewHopInfo(
                 8,
                 level: 1,
                 isSplitter: false,
                 isTraceSource: false,
                 node: "SP343344",
                 equipment: "FTTU",
                 terminalStructure: "Bakke 1",
                 terminal: "Søm 1",
                 connectionInfo: "Intern forb",
                 totalLength: 1434,
                 routeSegmentGeometries: Array.Empty<string>(),
                 routeSegmentIds: Array.Empty<Guid>()
             )
           );

            hops.Add(
            new ConnectivityTraceViewHopInfo(
                9,
                level: 1,
                isSplitter: false,
                isTraceSource: false,
                node: "IA12345678",
                equipment: "",
                terminalStructure: "",
                terminal: "",
                connectionInfo: "Engum Møllevej 3, Vejle (3442334)",
                totalLength: 1434,
                routeSegmentGeometries: Array.Empty<string>(),
                routeSegmentIds: Array.Empty<Guid>()
            )
          );

            // splitter ud 2

            hops.Add(
              new ConnectivityTraceViewHopInfo(
                  10,
                  level: 0,
                  isSplitter: true,
                  isTraceSource: false,
                  node: "F1200",
                  equipment: "RACK 1 - 1:32 Splitter",
                  terminalStructure: "Splitter 1",
                  terminal: "IND 1 / UD 2",
                  connectionInfo: "Intern forb",
                  totalLength: 484,
                  routeSegmentGeometries: Array.Empty<string>(),
                  routeSegmentIds: Array.Empty<Guid>()
              )
            );

            hops.Add(
             new ConnectivityTraceViewHopInfo(
                 11,
                 level: 1,
                 isSplitter: false,
                 isTraceSource: false,
                 node: "F1200",
                 equipment: "RACK 1 - GPS 2",
                 terminalStructure: "Bakke 1",
                 terminal: "Søm 12",
                 connectionInfo: "K12387546 (48) Fiber 11",
                 totalLength: 640,
                 routeSegmentGeometries: Array.Empty<string>(),
                 routeSegmentIds: Array.Empty<Guid>()
             )
           );

            hops.Add(
              new ConnectivityTraceViewHopInfo(
                  12,
                  level: 1,
                  isSplitter: false,
                  isTraceSource: false,
                  node: "F1230",
                  equipment: "BUDI 2s",
                  terminalStructure: "Bakke 1",
                  terminal: "Søm 12",
                  connectionInfo: "K12353434 (2) Fiber 1",
                  totalLength: 831,
                  routeSegmentGeometries: Array.Empty<string>(),
                  routeSegmentIds: Array.Empty<Guid>()
              )
            );

            hops.Add(
             new ConnectivityTraceViewHopInfo(
                 13,
                 level: 1,
                 isSplitter: false,
                 isTraceSource: false,
                 node: "SP343345",
                 equipment: "FTTU",
                 terminalStructure: "Bakke 1",
                 terminal: "Søm 1",
                 connectionInfo: "Intern forb",
                 totalLength: 831,
                 routeSegmentGeometries: Array.Empty<string>(),
                 routeSegmentIds: Array.Empty<Guid>()
             )
           );

            hops.Add(
            new ConnectivityTraceViewHopInfo(
                14,
                level: 1,
                isSplitter: false,
                isTraceSource: false,
                node: "IA300602",
                equipment: "",
                terminalStructure: "",
                terminal: "",
                connectionInfo: "Engum Møllevej 4, Vejle ",
                totalLength: 831,
                routeSegmentGeometries: Array.Empty<string>(),
                routeSegmentIds: Array.Empty<Guid>()
            )
          );


            return new ConnectivityTraceView("K12345678",hops.ToArray());
        }
     
    }
}
