using BallCom.Logistics.API.Data;
using BallCom.Logistics.API.Messaging;
using BallCom.Logistics.API.Models;
using BallCom.Logistics.API.Models.Events;
using BallCom.Logistics.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Logistics.API.Controllers
{
    [ApiController]
    [Route("api/shipments")]
    public class ShipmentsController : ControllerBase
    {
        private readonly LogisticsDbContext _context;
        private readonly CarrierStatusProvider _carrierStatusProvider;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<ShipmentsController> _logger;

        public ShipmentsController(
            LogisticsDbContext context,
            CarrierStatusProvider carrierStatusProvider,
            IEventPublisher eventPublisher,
            ILogger<ShipmentsController> logger)
        {
            _context = context;
            _carrierStatusProvider = carrierStatusProvider;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shipments = await _context.Shipments.AsNoTracking().OrderByDescending(s => s.CreatedAt).ToListAsync();
            return Ok(shipments);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var shipment = await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (shipment is null)
            {
                return NotFound();
            }

            return Ok(shipment);
        }

        [HttpGet("order/{orderId:int}/delivery-status")]
        public async Task<IActionResult> GetDeliveryStatus(int orderId)
        {
            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (shipment is null)
            {
                return NotFound($"Geen shipment gevonden voor order {orderId}.");
            }

            var previousStatus = shipment.Status;
            var carrierStatus = _carrierStatusProvider.FetchDeliveryStatus(shipment);

            if (carrierStatus != previousStatus)
            {
                var occurredAt = DateTime.UtcNow;
                var eventStore = new EventStore(_context);
                var statusEvent = new ShipmentStatusUpdatedEvent(
                    shipment.Id, shipment.OrderId, previousStatus, carrierStatus, shipment.SelectedCarrier, occurredAt);
                eventStore.Append(shipment.Id, nameof(Shipment), statusEvent);

                shipment.Status = carrierStatus;
                shipment.UpdatedAt = occurredAt;
                await _context.SaveChangesAsync();
                _eventPublisher.Publish(statusEvent);

                _logger.LogInformation("[Logistics Service] Delivery status order {OrderId}: {From} -> {To}.", orderId, previousStatus, carrierStatus);
            }

            return Ok(new
            {
                orderId,
                deliveryStatus = shipment.Status,
                carrier = shipment.SelectedCarrier,
                trackingNumber = shipment.TrackingNumber,
                selectedPrice = shipment.SelectedPrice,
                carrierQuotesAudit = shipment.CarrierQuotesAudit
            });
        }
    }
}
