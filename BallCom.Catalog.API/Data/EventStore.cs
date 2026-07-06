using System.Text.Json;
using BallCom.Catalog.API.Models;

namespace BallCom.Catalog.API.Data
{
    public class EventStore
    {
        private readonly CatalogDbContext _context;

        public EventStore(CatalogDbContext context)
        {
            _context = context;
        }

        public void Append<T>(Guid aggregateId, string aggregateType, T @event)
        {
            var stored = new StoredEvent
            {
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventType = typeof(T).Name,
                Payload = JsonSerializer.Serialize(@event),
                OccurredAt = DateTime.UtcNow
            };

            _context.EventStore.Add(stored);
        }
    }
}
