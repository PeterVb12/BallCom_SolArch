using BallCom.Ordering.API.Domain.Events;
using BallCom.Ordering.API.Models;

namespace BallCom.Ordering.API.Domain
{
    public class OrderAggregate
    {
        private readonly List<IOrderEvent> _uncommittedEvents = new();

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

        public int Version { get; private set; }
        private bool Exists => Version > 0 || _uncommittedEvents.Count > 0;

        private OrderAggregate() { }

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
                return;
            }

            if (Status == OrderStatus.Processing)
            {
                throw new InvalidOperationException("Een order die al in behandeling is kan niet meer geannuleerd worden.");
            }

            Raise(new OrderCancelledDomainEvent(Id, reason, DateTime.UtcNow));
        }

        private void Raise(IOrderEvent @event)
        {
            Apply(@event);
            _uncommittedEvents.Add(@event);
        }

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
