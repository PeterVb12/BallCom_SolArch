using BallCom.Warehouse.API.Data;
using BallCom.Warehouse.API.Messaging;
using BallCom.Warehouse.API.Models;
using BallCom.Warehouse.API.Models.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Warehouse.API.Controllers
{
    [ApiController]
    [Route("api/picklists")]
    public class PickListsController : ControllerBase
    {
        private readonly WarehouseDbContext _context;
        private readonly ILogger<PickListsController> _logger;
        private readonly IEventPublisher _eventPublisher;

        public PickListsController(WarehouseDbContext context,
                                   ILogger<PickListsController> logger,
                                   IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        [HttpGet]
        public async Task<IActionResult> GetPickLists()
        {
            var pickLists = await _context.PickLists
                                          .AsNoTracking()
                                          .Include(p => p.Lines)
                                          .ToListAsync();
            return Ok(pickLists);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPickListById(Guid id)
        {
            var pickList = await _context.PickLists
                                         .AsNoTracking()
                                         .Include(p => p.Lines)
                                         .FirstOrDefaultAsync(p => p.Id == id);
            if (pickList is null)
            {
                return NotFound();
            }

            return Ok(pickList);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetPickListByOrderId(int orderId)
        {
            var pickList = await _context.PickLists
                                         .AsNoTracking()
                                         .Include(p => p.Lines)
                                         .FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (pickList is null)
            {
                return NotFound();
            }

            return Ok(pickList);
        }

        [HttpPost("{id:guid}/start-picking")]
        public Task<IActionResult> StartPicking(Guid id)
            => Transition(id, PickListStatus.Released, PickListStatus.Picking);

        [HttpPost("{id:guid}/complete-picking")]
        public Task<IActionResult> CompletePicking(Guid id)
            => Transition(id, PickListStatus.Picking, PickListStatus.Picked);

        [HttpPost("{id:guid}/pack")]
        public Task<IActionResult> Pack(Guid id)
            => Transition(id, PickListStatus.Picked, PickListStatus.Packed);

        [HttpPost("{id:guid}/ready")]
        public Task<IActionResult> Ready(Guid id)
            => Transition(id, PickListStatus.Packed, PickListStatus.ReadyForShipment);

        private async Task<IActionResult> Transition(Guid id, string requiredStatus, string newStatus)
        {
            var pickList = await _context.PickLists.FirstOrDefaultAsync(p => p.Id == id);
            if (pickList is null)
            {
                return NotFound();
            }

            if (pickList.Status != requiredStatus)
            {
                return Conflict($"Ongeldige statusovergang: pick list heeft status '{pickList.Status}', maar '{requiredStatus}' is vereist voor deze actie.");
            }

            var occurredAt = DateTime.UtcNow;
            var eventStore = new EventStore(_context);

            var statusChanged = new PickListStatusChangedEvent(
                pickList.Id, pickList.OrderId, pickList.Status, newStatus, occurredAt);
            eventStore.Append(pickList.Id, nameof(PickList), statusChanged);

            pickList.Status = newStatus;
            pickList.UpdatedAt = occurredAt;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Warehouse Service] PickList {PickListId} -> {Status}.", pickList.Id, newStatus);

            if (newStatus == PickListStatus.ReadyForShipment)
            {
                var readyEvent = new PackageReadyEvent(pickList.Id, pickList.OrderId, occurredAt);
                _eventPublisher.Publish(readyEvent);
                _logger.LogInformation("[Warehouse Service] PackageReadyEvent gepubliceerd voor order {OrderId}.", pickList.OrderId);
            }

            return Ok(pickList);
        }
    }
}
