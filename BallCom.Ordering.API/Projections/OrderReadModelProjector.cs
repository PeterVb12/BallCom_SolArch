using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Domain.Events;
using BallCom.Ordering.API.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Projections
{
    // Projecteert domein-events naar de gedenormaliseerde read models (Q).
    // Wordt zowel live (OrderProjectionService) als bij een volledige rebuild
    // (ReadModelRebuilder) gebruikt, met exact dezelfde Apply-logica.
    public class OrderReadModelProjector
    {
        private readonly OrderingReadDbContext _read;

        public OrderReadModelProjector(OrderingReadDbContext read)
        {
            _read = read;
        }

        public async Task ProjectAsync(IEnumerable<IOrderEvent> events, CancellationToken ct = default)
        {
            foreach (var @event in events)
            {
                await ApplyAsync(@event, ct);
            }
            await _read.SaveChangesAsync(ct);
        }

        private async Task ApplyAsync(IOrderEvent @event, CancellationToken ct)
        {
            switch (@event)
            {
                case OrderPlacedDomainEvent e:
                    await ApplyPlacedAsync(e, ct);
                    break;

                case OrderPaidDomainEvent e:
                    await SetStatusAsync(e.OrderId, OrderStatusText.Paid, e.OccurredAt, ct);
                    break;

                case OrderProcessingStartedDomainEvent e:
                    await SetStatusAsync(e.OrderId, OrderStatusText.Processing, e.OccurredAt, ct);
                    break;

                case OrderCancelledDomainEvent e:
                    await SetStatusAsync(e.OrderId, OrderStatusText.Cancelled, e.OccurredAt, ct);
                    break;
            }
        }

        private async Task ApplyPlacedAsync(OrderPlacedDomainEvent e, CancellationToken ct)
        {
            // Idempotent: bestaat het overzicht al, dan is dit event al verwerkt.
            // FindAsync checkt ook de nog-niet-opgeslagen entiteiten in de tracker,
            // zodat een replay van meerdere events in één batch correct verloopt.
            var existing = await _read.OrderSummaries.FindAsync(new object?[] { e.OrderId }, ct);
            if (existing is not null)
            {
                return;
            }

            _read.OrderSummaries.Add(new OrderSummary
            {
                OrderId = e.OrderId,
                Status = OrderStatusText.Pending,
                TotalPrice = e.TotalPrice,
                CustomerEmail = e.CustomerEmail,
                CustomerName = e.CustomerName,
                Street = e.Street,
                City = e.City,
                PostalCode = e.PostalCode,
                Country = e.Country,
                ItemCount = e.Items.Sum(i => i.Quantity),
                PlacedAt = e.OccurredAt,
                LastUpdatedAt = e.OccurredAt
            });

            foreach (var line in e.Items)
            {
                _read.OrderLineViews.Add(new OrderLineView
                {
                    OrderId = e.OrderId,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    Price = line.Price
                });
            }

            // Tweede projectie: "aantal orders + besteed bedrag per klant".
            var stat = await _read.CustomerOrderStats.FindAsync(new object?[] { e.CustomerEmail }, ct);
            if (stat is null)
            {
                _read.CustomerOrderStats.Add(new CustomerOrderStat
                {
                    CustomerEmail = e.CustomerEmail,
                    OrderCount = 1,
                    TotalSpent = e.TotalPrice,
                    LastOrderAt = e.OccurredAt
                });
            }
            else
            {
                stat.OrderCount += 1;
                stat.TotalSpent += e.TotalPrice;
                stat.LastOrderAt = e.OccurredAt;
            }
        }

        private async Task SetStatusAsync(int orderId, string status, DateTime occurredAt, CancellationToken ct)
        {
            var summary = await _read.OrderSummaries.FindAsync(new object?[] { orderId }, ct);
            if (summary is null)
            {
                return;
            }

            summary.Status = status;
            summary.LastUpdatedAt = occurredAt;
        }
    }

    // Statustekst voor de leeskant (spiegelt Models.OrderStatus, maar houdt de
    // leeskant onafhankelijk van het schrijfmodel).
    public static class OrderStatusText
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Processing = "PROCESSING";
        public const string Cancelled = "CANCELLED";
    }
}
