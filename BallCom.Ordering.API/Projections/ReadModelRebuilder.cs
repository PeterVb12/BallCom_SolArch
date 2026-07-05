using System.Text.Json;
using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Domain;
using BallCom.Ordering.API.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Projections
{
    // Herbouwt de VOLLEDIGE leeskant (Q) vanuit de event store. Dit toont de kern
    // van event sourcing: de read models zijn wegwerpbaar en volledig af te leiden
    // door alle events opnieuw af te spelen. Handig na een schema-wijziging of om
    // een nieuwe projectie toe te voegen.
    public class ReadModelRebuilder
    {
        private readonly OrderingWriteDbContext _write;
        private readonly OrderingReadDbContext _read;
        private readonly ILogger<ReadModelRebuilder> _logger;

        public ReadModelRebuilder(
            OrderingWriteDbContext write,
            OrderingReadDbContext read,
            ILogger<ReadModelRebuilder> logger)
        {
            _write = write;
            _read = read;
            _logger = logger;
        }

        public async Task<int> RebuildAsync(CancellationToken ct = default)
        {
            _logger.LogWarning("[Ordering ES] START REBUILD: read models worden opnieuw opgebouwd vanuit de event store.");

            await _read.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"OrderSummaries\", \"OrderLineViews\", \"CustomerOrderStats\";", ct);

            var stored = await _write.OrderEvents
                .AsNoTracking()
                .Where(e => e.AggregateType == "Order")
                .OrderBy(e => e.Sequence)
                .ToListAsync(ct);

            var events = stored.Select(Deserialize).ToList();

            var projector = new OrderReadModelProjector(_read);
            await projector.ProjectAsync(events, ct);

            _logger.LogInformation("[Ordering ES] REBUILD KLAAR: {Count} events opnieuw afgespeeld.", events.Count);
            return events.Count;
        }

        private static IOrderEvent Deserialize(Models.StoredEvent stored)
        {
            var type = OrderEventTypeRegistry.Resolve(stored.EventType)
                ?? throw new InvalidOperationException($"Onbekend event-type: {stored.EventType}");
            return (IOrderEvent)JsonSerializer.Deserialize(stored.Payload, type)!;
        }
    }
}
