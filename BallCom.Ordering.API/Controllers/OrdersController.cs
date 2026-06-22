using BallCom.Ordering.API.Data;
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

        public OrdersController(OrderingDbContext context, ILogger<OrdersController> _logger)
        {
            _context = context;
            this._logger = _logger;
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

            _logger.LogInformation("📢 [Ordering Service] Order {OrderId} opgeslagen in database. Event 'OrderPlaced' gesimuleerd.", order.Id);

            return Ok(order);
        }
    }
}
