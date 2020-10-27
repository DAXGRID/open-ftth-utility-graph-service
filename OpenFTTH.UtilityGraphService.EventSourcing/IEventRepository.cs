using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.EventSourcing
{
    public interface IEventRepository
    {
        void Store(AggregateBase aggregate);
        T Load<T>(Guid id, int? version = null) where T : AggregateBase;
        bool CheckIfAggregateIdHasBeenUsed(Guid id);
    }
}
