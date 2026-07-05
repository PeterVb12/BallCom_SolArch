using BallCom.Ordering.API.Domain.Events;
using BallCom.Ordering.API.Models;

namespace BallCom.Ordering.API.Domain
{
    // ============================================================================
    //  EVENT SOURCING - de Order aggregate
    // ============================================================================
    //  De huidige staat van een order wordt NIET uit een tabelrij gelezen, maar
    //  volledig OPGEBOUWD door de events uit zijn event-stream opnieuw af te
    //  spelen in code (rehydratie). Dat gebeurt in Rehydrate(...) -> Apply(...).
    //
    //  Een command (Place / MarkPaid / Cancel) werkt zo:
    //    1. laad de aggregate door zijn events te replayen  (OrderEventStore.LoadAsync)
    //    2. controleer de business-regels tegen die gereconstrueerde staat
    //    3. raise een NIEUW event (AddEvent + Apply) i.p.v. de staat direct te muteren
    //    4. de nieuwe events worden append-only weggeschreven in de event store
    //
    //  Het heeft dus NIETS met queues te maken; het is puur het opnieuw
    //  afspelen van events in geheugen.
    // ============================================================================
    public class OrderAggregate
    {
        private readonly List<IOrderEvent> _uncommittedEvents = new();

        // Staat - uitsluitend het resultaat van afgespeelde events.
        public int Id { get; private set; }
        public string Status { get; private set; } = OrderStatus.Pending;
        public decimal TotalPrice { get; private set; }
        public string CustomerEmail { get; private set; } = string.Empty;
        public string CustomerName { get; private set; } = string.Empty;
        public string Street { get; private set; } = string.Empty;
        public string City { get; private set; } = string.Empty;
        public string PostalCode { get; private set; } = string.Empty;
        public string Country { get; private set; } = string.Empty;
        public List<OrderLineData> Items { get; private set; } = new();

        // Aantal events dat al persistent in de store staat (voor optimistic concurrency).
        public int Version { get; private set; }
        private bool Exists => Version > 0 || _uncommittedEvents.Count > 0;

        // Privé: alleen te maken via de factory Place(...) of via Rehydrate(...).
        private OrderAggregate() { }

        // ----- Rehydratie: bouw de aggregate op door events af te spelen -----
        public static OrderAggregate Rehydrate(IEnumerable<IOrderEvent> history)
        {
            var order = new OrderAggregate();
            foreach (var @event in history)
            {
                order.Apply(@event);
                order.Version++;
            }
            return order;
        }

        // ----- Commands (schrijfkant): valideren en NIEUWE events raisen -----

        public static OrderAggregate Place(
            int orderId,
            string customerEmail,
            string customerName,
            string street,
            string city,
            string postalCode,
            string country,
            IReadOnlyList<OrderLineData> items,
            decimal totalPrice)
        {
            if (items is null || items.Count == 0)
            {
                throw new InvalidOperationException("Een bestelling vereist minimaal 1 productregel.");
            }

            var order = new OrderAggregate();
            order.Raise(new OrderPlacedDomainEvent(
                orderId, customerEmail, customerName, street, city, postalCode, country,
                items, totalPrice, DateTime.UtcNow));
            return order;
        }

        public void MarkPaid(decimal amount)
        {
            if (!Exists)
            {
                throw new InvalidOperationException("Order bestaat niet.");
            }

            // Idempotent: al betaald? Dan geen nieuw event (voorkomt dubbele PAID-events
            // bij at-least-once bezorging van PaymentCompletedEvent).
            if (Status == OrderStatus.Paid || Status == OrderStatus.Processing)
            {
                return;
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Een geannuleerde order kan niet betaald worden.");
            }

            Raise(new OrderPaidDomainEvent(Id, amount, DateTime.UtcNow));
        }

        public void StartProcessing()
        {
            if (Status != OrderStatus.Paid)
            {
                throw new InvalidOperationException("Alleen een betaalde order kan in behandeling gaan.");
            }

            Raise(new OrderProcessingStartedDomainEvent(Id, DateTime.UtcNow));
        }

        public void Cancel(string reason)
        {
            if (!Exists)
            {
                throw new InvalidOperationException("Order bestaat niet.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                return; // idempotent
            }

            if (Status == OrderStatus.Processing)
            {
                throw new InvalidOperationException("Een order die al in behandeling is kan niet meer geannuleerd worden.");
            }

            Raise(new OrderCancelledDomainEvent(Id, reason, DateTime.UtcNow));
        }

        // ----- Event-plumbing -----

        private void Raise(IOrderEvent @event)
        {
            Apply(@event);
            _uncommittedEvents.Add(@event);
        }

        // De projectie van event -> in-memory staat. Zowel bij het raisen van een
        // nieuw event als bij rehydratie wordt exact dezelfde logica gebruikt.
        private void Apply(IOrderEvent @event)
        {
            switch (@event)
            {
                case OrderPlacedDomainEvent e:
                    Id = e.OrderId;
                    CustomerEmail = e.CustomerEmail;
                    CustomerName = e.CustomerName;
                    Street = e.Street;
                    City = e.City;
                    PostalCode = e.PostalCode;
                    Country = e.Country;
                    Items = e.Items.ToList();
                    TotalPrice = e.TotalPrice;
                    Status = OrderStatus.Pending;
                    break;

                case OrderPaidDomainEvent:
                    Status = OrderStatus.Paid;
                    break;

                case OrderProcessingStartedDomainEvent:
                    Status = OrderStatus.Processing;
                    break;

                case OrderCancelledDomainEvent:
                    Status = OrderStatus.Cancelled;
                    break;
            }
        }

        public IReadOnlyList<IOrderEvent> DequeueUncommittedEvents()
        {
            var events = _uncommittedEvents.ToList();
            _uncommittedEvents.Clear();
            return events;
        }
    }
}
