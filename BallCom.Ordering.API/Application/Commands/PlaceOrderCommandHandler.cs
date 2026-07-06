using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Domain;
using BallCom.Ordering.API.Domain.Events;
using BallCom.Ordering.API.Messaging;
using BallCom.Ordering.API.Models;
using BallCom.Ordering.API.Projections;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Application.Commands
{
    public class PlaceOrderCommandHandler
    {
        private readonly OrderEventStore _eventStore;
        private readonly OrderingWriteDbContext _write;
        private readonly ProjectionQueue _projectionQueue;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<PlaceOrderCommandHandler> _logger;

        public PlaceOrderCommandHandler(
            OrderEventStore eventStore,
            OrderingWriteDbContext write,
            ProjectionQueue projectionQueue,
            IEventPublisher eventPublisher,
            ILogger<PlaceOrderCommandHandler> logger)
        {
            _eventStore = eventStore;
            _write = write;
            _projectionQueue = projectionQueue;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<OrderAggregate> HandleAsync(CreateOrderCommand command)
        {
            var customer = command.Customer
                ?? throw new ArgumentException("Klantgegevens (customer) zijn verplicht voor levering en betaling (F05).");

            if (string.IsNullOrWhiteSpace(customer.Email) ||
                string.IsNullOrWhiteSpace(customer.FullName) ||
                string.IsNullOrWhiteSpace(customer.Street) ||
                string.IsNullOrWhiteSpace(customer.City) ||
                string.IsNullOrWhiteSpace(customer.PostalCode) ||
                string.IsNullOrWhiteSpace(customer.Country))
            {
                throw new ArgumentException("Alle klantgegevens (email, naam, adres) zijn verplicht.");
            }

            var (lines, totalPrice) = await ValidateItemsAsync(command.Items);

            var orderId = await _eventStore.NextOrderIdAsync();

            var aggregate = OrderAggregate.Place(
                orderId,
                customer.Email.Trim(),
                customer.FullName.Trim(),
                customer.Street.Trim(),
                customer.City.Trim(),
                customer.PostalCode.Trim(),
                customer.Country.Trim(),
                lines,
                totalPrice);

            var appended = await _eventStore.SaveAsync(aggregate);

            await _projectionQueue.EnqueueAsync(appended);

            _eventPublisher.Publish(new OrderPlacedEvent(orderId, totalPrice, DateTime.UtcNow));

            _logger.LogInformation("[Ordering ES] Order {OrderId} geplaatst (event opgeslagen + gepubliceerd).", orderId);

            return aggregate;
        }

        private async Task<(List<OrderLineData> Lines, decimal TotalPrice)> ValidateItemsAsync(List<OrderItemDto> itemDtos)
        {
            if (itemDtos is null || itemDtos.Count == 0)
            {
                throw new ArgumentException("Een bestelling vereist minimaal 1 productregel.");
            }

            if (itemDtos.Count > 20)
            {
                throw new ArgumentException("Een bestelling mag maximaal 20 verschillende producten bevatten.");
            }

            var lines = new List<OrderLineData>();
            decimal total = 0;

            foreach (var itemDto in itemDtos)
            {
                if (!Guid.TryParse(itemDto.ProductId, out var productGuid))
                {
                    throw new ArgumentException($"Ongeldig ProductId formaat: {itemDto.ProductId}");
                }

                var localProduct = await _write.Products.FindAsync(productGuid);
                if (localProduct is null)
                {
                    throw new ArgumentException($"Product met ID {itemDto.ProductId} bestaat niet in onze catalogus-referentie.");
                }

                if (itemDto.Quantity < 1)
                {
                    throw new ArgumentException($"Quantity moet minimaal 1 zijn voor product {itemDto.ProductId}.");
                }

                lines.Add(new OrderLineData(itemDto.ProductId, itemDto.Quantity, localProduct.Price));
                total += localProduct.Price * itemDto.Quantity;
            }

            return (lines, total);
        }
    }
}
