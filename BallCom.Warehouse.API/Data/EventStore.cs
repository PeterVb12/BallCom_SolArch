using System.Text.Json;
using BallCom.Warehouse.API.Models;

namespace BallCom.Warehouse.API.Data
{
    public class EventStore
    {
        private readonly WarehouseDbContext _context;

        public EventStore(WarehouseDbContext context)
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
