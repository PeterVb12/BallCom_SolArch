using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Messaging;
using BallCom.Ordering.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderingDbContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IEventPublisher _eventPublisher;

        public OrdersController(OrderingDbContext context,
                                ILogger<OrdersController> logger,
                                IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
        {
            var customerError = ValidateCustomer(command.Customer);
            if (customerError is not null)
            {
                return customerError;
            }

            var validation = await ValidateItemsAsync(command.Items);
            if (validation.Error is not null)
            {
                return validation.Error;
            }

            var order = new Order
            {
                Items = validation.Items!,
                TotalPrice = validation.TotalPrice,
                Status = OrderStatus.Pending,
                CustomerEmail = command.Customer.Email.Trim(),
                CustomerName = command.Customer.FullName.Trim(),
                Street = command.Customer.Street.Trim(),
                City = command.Customer.City.Trim(),
                PostalCode = command.Customer.PostalCode.Trim(),
                Country = command.Customer.Country.Trim()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderPlacedEvent = new OrderPlacedEvent(order.Id, order.TotalPrice, DateTime.UtcNow);
            _eventPublisher.Publish(orderPlacedEvent);

            _logger.LogInformation("[Ordering Service] Order {OrderId} opgeslagen met klantgegevens. OrderPlacedEvent gepubliceerd.", order.Id);

            return Ok(order);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _context.Orders
                                      .AsNoTracking()
                                      .Include(o => o.Items)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        /// <summary>F13: orderstatus ophalen bij de Order service.</summary>
        [HttpGet("{id:int}/status")]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var order = await _context.Orders
                                      .AsNoTracking()
                                      .Include(o => o.Items)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return NotFound();
            }

            return Ok(new
            {
                orderId = order.Id,
                orderStatus = order.Status,
                totalPrice = order.TotalPrice,
                customerEmail = order.CustomerEmail,
                customerName = order.CustomerName,
                deliveryAddress = new
                {
                    order.Street,
                    order.City,
                    order.PostalCode,
                    order.Country
                },
                items = order.Items.Select(i => new { i.ProductId, i.Quantity, i.Price })
            });
        }

        private static IActionResult? ValidateCustomer(CustomerDetailsDto? customer)
        {
            if (customer is null)
            {
                return new BadRequestObjectResult("Klantgegevens (customer) zijn verplicht voor levering en betaling (F05).");
            }

            if (string.IsNullOrWhiteSpace(customer.Email) ||
                string.IsNullOrWhiteSpace(customer.FullName) ||
                string.IsNullOrWhiteSpace(customer.Street) ||
                string.IsNullOrWhiteSpace(customer.City) ||
                string.IsNullOrWhiteSpace(customer.PostalCode) ||
                string.IsNullOrWhiteSpace(customer.Country))
            {
                return new BadRequestObjectResult("Alle klantgegevens (email, naam, adres) zijn verplicht.");
            }

            return null;
        }

        private async Task<(List<OrderItem>? Items, decimal TotalPrice, IActionResult? Error)> ValidateItemsAsync(
            List<OrderItemDto> itemDtos)
        {
            if (itemDtos is null || itemDtos.Count == 0)
            {
                return (null, 0, BadRequest("Een bestelling vereist minimaal 1 productregel."));
            }

            if (itemDtos.Count > 20)
            {
                return (null, 0, BadRequest("Een bestelling mag maximaal 20 verschillende producten bevatten."));
            }

            var validatedItems = new List<OrderItem>();
            decimal calculatedTotalPrice = 0;

            foreach (var itemDto in itemDtos)
            {
                if (!Guid.TryParse(itemDto.ProductId, out _))
                {
                    return (null, 0, BadRequest($"Ongeldig ProductId formaat: {itemDto.ProductId}"));
                }

                var localProduct = await _context.Products.FindAsync(Guid.Parse(itemDto.ProductId));
                if (localProduct is null)
                {
                    return (null, 0, BadRequest($"Product met ID {itemDto.ProductId} bestaat niet in onze catalogus-referentie."));
                }

                if (itemDto.Quantity < 1)
                {
                    return (null, 0, BadRequest($"Quantity moet minimaal 1 zijn voor product {itemDto.ProductId}."));
                }

                validatedItems.Add(new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    Price = localProduct.Price
                });

                calculatedTotalPrice += localProduct.Price * itemDto.Quantity;
            }

            return (validatedItems, calculatedTotalPrice, null);
        }
    }
}
