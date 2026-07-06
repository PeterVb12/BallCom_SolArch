using BallCom.Ordering.API.Application.Commands;
using BallCom.Ordering.API.Application.Queries;
using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Domain;
using BallCom.Ordering.API.Models;
using BallCom.Ordering.API.Projections;
using Microsoft.AspNetCore.Mvc;

namespace BallCom.Ordering.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly PlaceOrderCommandHandler _placeOrder;
        private readonly CancelOrderCommandHandler _cancelOrder;
        private readonly OrderQueryHandler _queries;
        private readonly OrderEventStore _eventStore;
        private readonly ReadModelRebuilder _rebuilder;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            PlaceOrderCommandHandler placeOrder,
            CancelOrderCommandHandler cancelOrder,
            OrderQueryHandler queries,
            OrderEventStore eventStore,
            ReadModelRebuilder rebuilder,
            ILogger<OrdersController> logger)
        {
            _placeOrder = placeOrder;
            _cancelOrder = cancelOrder;
            _queries = queries;
            _eventStore = eventStore;
            _rebuilder = rebuilder;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
        {
            try
            {
                var order = await _placeOrder.HandleAsync(command);
                return Ok(BuildOrderResponse(order));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelReason? body)
        {
            try
            {
                var ok = await _cancelOrder.HandleAsync(new CancelOrderCommand(id, body?.Reason ?? "Geannuleerd"));
                return ok ? Ok() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var view = await _queries.GetByIdAsync(id);
            if (view is not null)
            {
                return Ok(BuildOrderResponse(view));
            }

            var aggregate = await _eventStore.LoadAsync(id);
            return aggregate is null ? NotFound() : Ok(BuildOrderResponse(aggregate));
        }

        [HttpGet("{id:int}/status")]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var view = await _queries.GetByIdAsync(id);
            if (view is null)
            {
                var aggregate = await _eventStore.LoadAsync(id);
                if (aggregate is null)
                {
                    return NotFound();
                }
                return Ok(BuildStatusResponse(aggregate));
            }

            return Ok(BuildStatusResponse(view));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _queries.GetAllAsync());

        [HttpGet("stats/customers")]
        public async Task<IActionResult> GetCustomerStats()
            => Ok(await _queries.GetCustomerStatsAsync());

        [HttpGet("{id:int}/events")]
        public async Task<IActionResult> GetEvents(int id)
        {
            var stream = await _eventStore.ReadRawStreamAsync(id);
            if (stream.Count == 0)
            {
                return NotFound();
            }

            return Ok(stream.Select(e => new
            {
                e.Sequence,
                e.StreamId,
                e.Version,
                e.EventType,
                e.OccurredAt,
                e.Payload
            }));
        }

        [HttpPost("replay")]
        public async Task<IActionResult> Replay()
        {
            var count = await _rebuilder.RebuildAsync();
            return Ok($"Read models herbouwd vanuit {count} events.");
        }

        private static object BuildOrderResponse(OrderAggregate order) => new
        {
            id = order.Id,
            totalPrice = order.TotalPrice,
            status = order.Status,
            customerEmail = order.CustomerEmail,
            customerName = order.CustomerName,
            items = order.Items.Select(i => new { productId = i.ProductId, quantity = i.Quantity, price = i.Price })
        };

        private static object BuildOrderResponse(OrderView view) => new
        {
            id = view.Summary.OrderId,
            totalPrice = view.Summary.TotalPrice,
            status = view.Summary.Status,
            customerEmail = view.Summary.CustomerEmail,
            customerName = view.Summary.CustomerName,
            items = view.Lines.Select(i => new { productId = i.ProductId, quantity = i.Quantity, price = i.Price })
        };

        private static object BuildStatusResponse(OrderAggregate order) => new
        {
            orderId = order.Id,
            orderStatus = order.Status,
            totalPrice = order.TotalPrice,
            customerEmail = order.CustomerEmail,
            customerName = order.CustomerName,
            deliveryAddress = new { order.Street, order.City, order.PostalCode, order.Country },
            items = order.Items.Select(i => new { i.ProductId, i.Quantity, i.Price })
        };

        private static object BuildStatusResponse(OrderView view) => new
        {
            orderId = view.Summary.OrderId,
            orderStatus = view.Summary.Status,
            totalPrice = view.Summary.TotalPrice,
            customerEmail = view.Summary.CustomerEmail,
            customerName = view.Summary.CustomerName,
            deliveryAddress = new
            {
                view.Summary.Street,
                view.Summary.City,
                view.Summary.PostalCode,
                view.Summary.Country
            },
            items = view.Lines.Select(i => new { i.ProductId, i.Quantity, i.Price })
        };
    }

    public record CancelReason(string Reason);
}
