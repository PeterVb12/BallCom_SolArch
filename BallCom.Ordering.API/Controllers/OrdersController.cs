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
            // DDD Business Rule Check (Maximaal 20 items in totaal)
            if (command.Items.Sum(i => i.Quantity) > 20)
            {
                return BadRequest("Je mag maximaal 20 items per keer bestellen.");
            }

            // Lijst waarin we de gecontroleerde orderregels gaan stoppen
            var validatedItems = new List<OrderItem>();
            decimal calculatedTotalPrice = 0;

            // Loop door de items uit de frontend en valideer ze tegen onze lokale database
            foreach (var itemDto in command.Items)
            {
                // We proberen het product via zijn GUID te zoeken in onze lokale referentietabel
                if (!Guid.TryParse(itemDto.ProductId, out Guid productGuid))
                {
                    return BadRequest($"Ongeldig ProductId formaat: {itemDto.ProductId}");
                }

                var localProduct = await _context.Products.FindAsync(productGuid);

                if (localProduct == null)
                {
                    return BadRequest($"Product met ID {itemDto.ProductId} bestaat niet in onze catalogus-referentie.");
                }

                // !! We pakken de prijs uit ONZE database, NIET uit het request van de frontend!
                var realPrice = localProduct.Price;

                validatedItems.Add(new OrderItem 
                { 
                    ProductId = itemDto.ProductId, 
                    Quantity = itemDto.Quantity, 
                    Price = realPrice // Beveiligd!
                });

                calculatedTotalPrice += (realPrice * itemDto.Quantity);
            }

            // Maak de order aan met de gevalideerde data
            var order = new Order
            {
                Items = validatedItems,
                TotalPrice = calculatedTotalPrice,
                Status = "PENDING"
            };

            // Opslaan in de échte Postgres database in Docker!
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Publiceer het event over de bus
            var orderPlacedEvent = new OrderPlacedEvent(order.Id, order.TotalPrice, DateTime.UtcNow);
            _eventPublisher.Publish(orderPlacedEvent);
            
            _logger.LogInformation("[Ordering Service] Order {OrderId} succesvol en veilig opgeslagen. Totaalprijs: {TotalPrice}. Event 'OrderPlaced' gepubliceerd.", order.Id, order.TotalPrice);

            return Ok(order);
        }
    }
}
