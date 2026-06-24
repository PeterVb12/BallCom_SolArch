using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Messaging;
using BallCom.Catalog.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    public class SuppliersController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<SuppliersController> _logger;
        private readonly IEventPublisher _eventPublisher;

        public SuppliersController(CatalogDbContext context,
                                   ILogger<SuppliersController> logger,
                                   IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        // CQRS - COMMAND: registreer een vertrouwde supplier.
        [HttpPost]
        public async Task<IActionResult> RegisterSupplier([FromBody] RegisterSupplierCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.ContactEmail))
            {
                return BadRequest("Een supplier vereist minimaal een naam en een contact-e-mail.");
            }

            var supplierId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var supplierRegisteredEvent = new SupplierRegisteredEvent(
                supplierId, command.Name, command.ContactEmail, occurredAt);

            // Event Sourcing: leg de registratie vast als feit in de event store.
            var eventStore = new EventStore(_context);
            eventStore.Append(supplierId, nameof(TrustedSupplier), supplierRegisteredEvent);

            var supplier = new TrustedSupplier
            {
                Id = supplierId,
                Name = command.Name,
                ContactEmail = command.ContactEmail,
                RegisteredAt = occurredAt
            };
            _context.Suppliers.Add(supplier);

            await _context.SaveChangesAsync();

            _eventPublisher.Publish(supplierRegisteredEvent);
            _logger.LogInformation("[Catalog Service] Supplier {SupplierId} geregistreerd en event 'SupplierRegisteredEvent' gepubliceerd.", supplierId);

            return CreatedAtAction(nameof(GetSuppliers), new { id = supplierId }, supplier);
        }

        // CQRS - QUERY: lijst van vertrouwde suppliers.
        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            var suppliers = await _context.Suppliers.AsNoTracking().ToListAsync();
            return Ok(suppliers);
        }
    }
}
