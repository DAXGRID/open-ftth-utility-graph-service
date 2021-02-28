using FluentResults;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.UtilityGraphService.API.Commands;
using System.Threading.Tasks;

namespace OpenFTTH.UtilityGraphService.Business.SpanEquipments.CommandHandlers
{
    public class CutSpanSegmentsCommandHandler : ICommandHandler<CutSpanSegmentsAtRouteNode, Result>
    {
        private readonly IEventStore _eventStore;

        public CutSpanSegmentsCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<Result> HandleAsync(CutSpanSegmentsAtRouteNode command)
        {
            var spanEquipmentAR = _eventStore.Aggregates.Load<SpanEquipmentAR>(command.SpanEquipmentId);

            var cuteSpanEquipmentsResult = spanEquipmentAR.CutSpanSegments(
                command.RouteNodeId, 
                command.SpanSegmentsToCut
            );

            if (cuteSpanEquipmentsResult.IsSuccess)
            {
                _eventStore.Aggregates.Store(spanEquipmentAR);
            }

            return Task.FromResult(cuteSpanEquipmentsResult);
        }
    }
}

  