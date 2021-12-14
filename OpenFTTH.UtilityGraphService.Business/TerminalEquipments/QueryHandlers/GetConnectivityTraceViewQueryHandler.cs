using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Model.UtilityNetwork.Views;
using OpenFTTH.UtilityGraphService.API.Queries;
using OpenFTTH.UtilityGraphService.Business.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.TerminalEquipments.QueryHandling
{
    public class GetConnectivityTraceQueryHandler
        : IQueryHandler<GetConnectivityTraceView, Result<ConnectivityTraceView>>
    {
        private readonly IEventStore _eventStore;
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly UtilityNetworkProjection _utilityNetwork;

        public GetConnectivityTraceQueryHandler(IEventStore eventStore, IQueryDispatcher queryDispatcher)
        {
            _eventStore = eventStore;
            _queryDispatcher = queryDispatcher;
            _utilityNetwork = _eventStore.Projections.Get<UtilityNetworkProjection>();
        }

        public Task<Result<ConnectivityTraceView>> HandleAsync(GetConnectivityTraceView query)
        {
            return Task.FromResult(BuildConnectivityTrace());
        }

        private Result<ConnectivityTraceView> BuildConnectivityTrace()
        {
            return Result.Ok(BuildTestData());
        }

        private ConnectivityTraceView BuildTestData()
        {
            List<ConnectivityTraceViewHopInfo> hops = new();

            hops.Add(
                new ConnectivityTraceViewHopInfo(
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
