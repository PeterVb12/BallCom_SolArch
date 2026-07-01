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
            if (command.Items.Sum(i => i.Quantity) > 20)
            {
                return BadRequest("Je mag maximaal 20 items per keer bestellen.");
            }

            var validatedItems = new List<OrderItem>();
            decimal calculatedTotalPrice = 0;

            foreach (var itemDto in command.Items)
            {
                if (!Guid.TryParse(itemDto.ProductId, out Guid productGuid))
                {
                    return BadRequest($"Ongeldig ProductId formaat: {itemDto.ProductId}");
                }

                var localProduct = await _context.Products.FindAsync(productGuid);

                if (localProduct == null)
                {
                    return BadRequest($"Product met ID {itemDto.ProductId} bestaat niet in onze catalogus-referentie.");
                }

                var realPrice = localProduct.Price;

                validatedItems.Add(new OrderItem 
                { 
                    ProductId = itemDto.ProductId, 
                    Quantity = itemDto.Quantity, 
                    Price = realPrice
                });

                calculatedTotalPrice += (realPrice * itemDto.Quantity);
            }

            var order = new Order
            {
                Items = validatedItems,
                TotalPrice = calculatedTotalPrice,
                Status = "PENDING"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderPlacedEvent = new OrderPlacedEvent(order.Id, order.TotalPrice, DateTime.UtcNow);
            _eventPublisher.Publish(orderPlacedEvent);
            
            _logger.LogInformation("[Ordering Service] Order {OrderId} succesvol en veilig opgeslagen. Totaalprijs: {TotalPrice}. Event 'OrderPlaced' gepubliceerd.", order.Id, order.TotalPrice);

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
    }
}
