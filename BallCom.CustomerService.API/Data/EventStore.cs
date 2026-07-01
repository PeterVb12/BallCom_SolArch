using System.Text.Json;
using BallCom.CustomerService.API.Models;

namespace BallCom.CustomerService.API.Data
{
    public class EventStore
    {
        private readonly CustomerServiceDbContext _context;

        public EventStore(CustomerServiceDbContext context)
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
