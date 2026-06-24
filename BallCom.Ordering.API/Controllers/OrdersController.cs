using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Messaging;
using BallCom.Ordering.API.Models;
using Microsoft.AspNetCore.Mvc;

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
                                ILogger<OrdersController> _logger,
                                IEventPublisher eventPublisher)
        {
            _context = context;
            this._logger = _logger;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
        {
            // DDD Business Rule Check
            if (command.Items.Sum(i => i.Quantity) > 20)
            {
                return BadRequest("Je mag maximaal 20 items per keer bestellen.");
            }

            var order = new Order
            {
                Items = command.Items.Select(i => new OrderItem { ProductId = i.ProductId, Quantity = i.Quantity, Price = i.Price }).ToList(),
                TotalPrice = command.Items.Sum(i => i.Price * i.Quantity)
            };

            // Opslaan in de échte Postgres database in Docker!
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderPlacedEvent = new OrderPlacedEvent(order.Id, order.TotalPrice, DateTime.UtcNow);

            _eventPublisher.Publish(orderPlacedEvent);
            _logger.LogInformation("Ordering Service] Order {OrderId} opgeslagen in database. Event 'OrderPlaced' gesimuleerd.", order.Id);

            return Ok(order);
        }
    }
}
