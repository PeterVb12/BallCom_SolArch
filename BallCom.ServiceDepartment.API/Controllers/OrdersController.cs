using System.Text;
using System.Text.Json;
using BallCom.ServiceDepartment.API.Data;
using BallCom.ServiceDepartment.API.Models;
using BallCom.ServiceDepartment.API.Models.Commands;
using BallCom.ServiceDepartment.API.Models.Events;
using BallCom.ServiceDepartment.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.ServiceDepartment.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ServiceDepartmentDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OrderStatusAggregator _statusAggregator;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ServiceDepartmentDbContext context,
            IHttpClientFactory httpClientFactory,
            OrderStatusAggregator statusAggregator,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _statusAggregator = statusAggregator;
            _logger = logger;
        }

        [HttpGet("{orderId:int}/status")]
        public async Task<IActionResult> GetStatus(int orderId)
        {
            var status = await _statusAggregator.GetOrderStatusAsync(orderId);
            if (status is null)
            {
                return NotFound($"Geen order gevonden met id {orderId}.");
            }

            return Ok(status);
        }

        [HttpPost("{orderId:int}/cancel")]
        public async Task<IActionResult> Cancel(int orderId, [FromBody] ServiceCancelOrderCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.EmployeeId) ||
                string.IsNullOrWhiteSpace(command.EmployeeName) ||
                string.IsNullOrWhiteSpace(command.Reason))
            {
                return BadRequest("EmployeeId, EmployeeName en Reason zijn verplicht.");
            }

            if (!await _statusAggregator.CanCancelOrModifyAsync(orderId))
            {
                return Conflict("Deze order kan niet meer geannuleerd worden: het pakket is klaar voor verzending of al geannuleerd.");
            }

            var orderingClient = _httpClientFactory.CreateClient("OrderingService");
            var payload = JsonSerializer.Serialize(new { reason = command.Reason });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await orderingClient.PostAsync($"api/orders/{orderId}/cancel", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }

            var auditId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;
            var auditedEvent = new OrderCancellationAuditedEvent(
                auditId, orderId, command.EmployeeId.Trim(), command.EmployeeName.Trim(), command.Reason.Trim(), occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(auditId, nameof(OrderAuditEntry), auditedEvent);

            _context.OrderAuditEntries.Add(new OrderAuditEntry
            {
                Id = auditId,
                OrderId = orderId,
                Action = AuditAction.Cancelled,
                EmployeeId = command.EmployeeId.Trim(),
                EmployeeName = command.EmployeeName.Trim(),
                Reason = command.Reason.Trim(),
                OccurredAt = occurredAt
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Service Department] Order {OrderId} geannuleerd door {Employee} ({EmployeeId}). Reden: {Reason}.",
                orderId, command.EmployeeName, command.EmployeeId, command.Reason);

            var orderJson = await response.Content.ReadAsStringAsync();
            return Content(orderJson, "application/json");
        }

        [HttpPut("{orderId:int}")]
        public async Task<IActionResult> Modify(int orderId, [FromBody] ServiceModifyOrderCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.EmployeeId) ||
                string.IsNullOrWhiteSpace(command.EmployeeName) ||
                string.IsNullOrWhiteSpace(command.Reason) ||
                command.Items is null || command.Items.Count == 0)
            {
                return BadRequest("EmployeeId, EmployeeName, Reason en Items zijn verplicht.");
            }

            if (!await _statusAggregator.CanCancelOrModifyAsync(orderId))
            {
                return Conflict("Deze order kan niet meer gewijzigd worden: het pakket is klaar voor verzending of al geannuleerd.");
            }

            var orderingClient = _httpClientFactory.CreateClient("OrderingService");
            var modifyPayload = JsonSerializer.Serialize(new { items = command.Items });
            var content = new StringContent(modifyPayload, Encoding.UTF8, "application/json");
            var response = await orderingClient.PutAsync($"api/orders/{orderId}", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }

            var auditId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;
            var details = JsonSerializer.Serialize(command.Items);
            var auditedEvent = new OrderModificationAuditedEvent(
                auditId, orderId, command.EmployeeId.Trim(), command.EmployeeName.Trim(), command.Reason.Trim(), details, occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(auditId, nameof(OrderAuditEntry), auditedEvent);

            _context.OrderAuditEntries.Add(new OrderAuditEntry
            {
                Id = auditId,
                OrderId = orderId,
                Action = AuditAction.Modified,
                EmployeeId = command.EmployeeId.Trim(),
                EmployeeName = command.EmployeeName.Trim(),
                Reason = command.Reason.Trim(),
                Details = details,
                OccurredAt = occurredAt
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Service Department] Order {OrderId} gewijzigd door {Employee} ({EmployeeId}). Reden: {Reason}.",
                orderId, command.EmployeeName, command.EmployeeId, command.Reason);

            var orderJson = await response.Content.ReadAsStringAsync();
            return Content(orderJson, "application/json");
        }

        [HttpGet("{orderId:int}/audit")]
        public async Task<IActionResult> GetAuditTrail(int orderId)
        {
            var entries = await _context.OrderAuditEntries
                                        .AsNoTracking()
                                        .Where(e => e.OrderId == orderId)
                                        .OrderByDescending(e => e.OccurredAt)
                                        .ToListAsync();

            return Ok(entries);
        }
    }
}
