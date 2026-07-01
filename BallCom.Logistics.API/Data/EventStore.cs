using System.Text.Json;
using BallCom.Logistics.API.Models;

namespace BallCom.Logistics.API.Data
{
    public class EventStore
    {
        private readonly LogisticsDbContext _context;

        public EventStore(LogisticsDbContext context)
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
