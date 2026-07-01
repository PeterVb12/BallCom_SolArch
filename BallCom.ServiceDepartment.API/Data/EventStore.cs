using System.Text.Json;
using BallCom.ServiceDepartment.API.Models;

namespace BallCom.ServiceDepartment.API.Data
{
    public class EventStore
    {
        private readonly ServiceDepartmentDbContext _context;

        public EventStore(ServiceDepartmentDbContext context)
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
