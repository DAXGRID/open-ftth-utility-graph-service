using Marten;
using Marten.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenFTTH.UtilityGraphService.EventSourcing
{
    public sealed class PostgresRepository : IEventRepository
    {
        private readonly IDocumentStore store;

        public PostgresRepository(IDocumentStore store)
        {
            this.store = store;
        }

        public void Store(AggregateBase aggregate)
        {
            using (var session = store.OpenSession())
            {
                // Take non-persisted events, push them to the event stream, indexed by the aggregate ID
                var events = aggregate.GetUncommittedEvents().ToArray();
                session.Events.Append(aggregate.Id, aggregate.Version, events);
                session.SaveChanges();
            }
            // Once succesfully persisted, clear events from list of uncommitted events
            aggregate.ClearUncommittedEvents();
        }

        private static readonly MethodInfo ApplyEvent = typeof(AggregateBase).GetMethod("ApplyEvent", BindingFlags.Instance | BindingFlags.NonPublic);

        public T Load<T>(Guid id, int? version = null) where T : AggregateBase
        {
            IReadOnlyList<IEvent> events;
            using (var session = store.LightweightSession())
            {
                events = session.Events.FetchStream(id, version ?? 0);
            }

            if (events != null && events.Any())
            {
                var instance = Activator.CreateInstance(typeof(T), true);
                // Replay our aggregate state from the event stream
                events.Aggregate(instance, (o, @event) => ApplyEvent.Invoke(instance, new[] { @event.Data }));
                return (T)instance;
            }

            throw new InvalidOperationException($"No aggregate by id {id}.");
        }

        public bool CheckIfAggregateIdHasBeenUsed(Guid id)
        {
            IReadOnlyList<IEvent> events;
            using (var session = store.LightweightSession())
            {
                events = session.Events.FetchStream(id);
            }

            if (events == null || (!events.Any()))
                return false;
            else
                return true;
        }
    }
}
