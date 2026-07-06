using System.Text.Json;
using BallCom.Ordering.API.Domain;
using BallCom.Ordering.API.Domain.Events;
using BallCom.Ordering.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Data
{
    public class OrderEventStore
    {
        private const string AggregateType = "Order";
        private readonly OrderingWriteDbContext _context;

        public OrderEventStore(OrderingWriteDbContext context)
        {
            _context = context;
        }

        public async Task<int> NextOrderIdAsync()
        {
            var ids = await _context.Database
                .SqlQueryRaw<int>("SELECT nextval('order_id_seq')::int AS \"Value\"")
                .ToListAsync();
            return ids[0];
        }

        public async Task<OrderAggregate?> LoadAsync(int orderId)
        {
            var streamId = orderId.ToString();

            var stored = await _context.OrderEvents
                .AsNoTracking()
                .Where(e => e.StreamId == streamId && e.AggregateType == AggregateType)
                .OrderBy(e => e.Version)
                .ToListAsync();

            if (stored.Count == 0)
            {
                return null;
            }

            var history = stored.Select(Deserialize).ToList();
            return OrderAggregate.Rehydrate(history);
        }

        public async Task<IReadOnlyList<StoredEvent>> ReadRawStreamAsync(int orderId)
        {
            var streamId = orderId.ToString();
            return await _context.OrderEvents
                .AsNoTracking()
                .Where(e => e.StreamId == streamId && e.AggregateType == AggregateType)
                .OrderBy(e => e.Version)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<IOrderEvent>> SaveAsync(OrderAggregate aggregate)
        {
            var newEvents = aggregate.DequeueUncommittedEvents();
            if (newEvents.Count == 0)
            {
                return newEvents;
            }

            var version = aggregate.Version;
            foreach (var @event in newEvents)
            {
                version++;
                _context.OrderEvents.Add(new StoredEvent
                {
                    StreamId = @event.OrderId.ToString(),
                    AggregateType = AggregateType,
                    Version = version,
                    EventType = @event.GetType().Name,
                    Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                    OccurredAt = @event.OccurredAt
                });
            }

            await _context.SaveChangesAsync();

            return newEvents;
        }

        private static IOrderEvent Deserialize(StoredEvent stored)
        {
            var type = OrderEventTypeRegistry.Resolve(stored.EventType)
                ?? throw new InvalidOperationException($"Onbekend event-type in de store: {stored.EventType}");

            return (IOrderEvent)JsonSerializer.Deserialize(stored.Payload, type)!;
        }
    }
}
