using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Messaging;
using BallCom.Catalog.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly IEventPublisher _eventPublisher;

        public ProductsController(CatalogDbContext context,
                                  ILogger<ProductsController> logger,
                                  IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        // ---------------------------------------------------------------
        // CQRS - COMMAND zijde (schrijven)
        // ---------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] AddProductCommand command)
        {
            // DDD Business Rule: een product moet minimaal naam, prijs en supplierId hebben.
            if (string.IsNullOrWhiteSpace(command.Name) || command.Price <= 0 || command.SupplierId == Guid.Empty)
            {
                return BadRequest("Een product vereist minimaal een naam, een prijs > 0 en een supplierId.");
            }

            // DDD Business Rule: alleen vertrouwde (geregistreerde) suppliers mogen producten toevoegen.
            var supplierIsTrusted = await _context.Suppliers.AnyAsync(s => s.Id == command.SupplierId);
            if (!supplierIsTrusted)
            {
                return BadRequest("Alleen vertrouwde suppliers mogen producten toevoegen aan de catalogus.");
            }

            // Controleer of deze leverancier dit product (op basis van naam) al eens heeft toegevoegd
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == command.Name.ToLower() && p.SupplierId == command.SupplierId);

            if (existingProduct != null)
            {
                // Het product bestaat al! We verhogen de voorraad (Stock)
                existingProduct.Stock += command.Stock;
                
                // We updaten de database
                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[Catalog Service] Product '{Name}' bestond al. Voorraad opgehoogd met {Stock}. Nieuwe totale voorraad: {TotalStock}", 
                    existingProduct.Name, command.Stock, existingProduct.Stock);

                // OPMERKING VOOR STRAKS: In een volwaardig EDA systeem zouden we nu een 'ProductStockIncreasedEvent' sturen.
                // Voor nu returnen we de geüpdatete versie van het bestaande product.
                return Ok(existingProduct);
            }

            var productId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var productAddedEvent = new ProductAddedEvent(
                productId, command.Name, command.Description, command.Price, command.Stock, command.SupplierId, occurredAt);

            // Event Sourcing: sla de mutatie eerst op als feit in de event store...
            var eventStore = new EventStore(_context);
            eventStore.Append(productId, nameof(Product), productAddedEvent);

            // ...en projecteer het read model (Products tabel) vanuit dat event.
            var product = Apply(new Product(), productAddedEvent);//CQRS
            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            // Event Driven Architecture: publiceer het event zodat downstream
            // services (Ordering) eventueel consistent kunnen bijwerken.
            _eventPublisher.Publish(productAddedEvent);
            _logger.LogInformation("[Catalog Service] Product {ProductId} toegevoegd en event 'ProductAddedEvent' gepubliceerd.", productId);

            return CreatedAtAction(nameof(GetProductById), new { id = productId }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(command.Name) || command.Price <= 0)
            {
                return BadRequest("Een product vereist minimaal een naam en een prijs > 0.");
            }

            var occurredAt = DateTime.UtcNow;
            var productUpdatedEvent = new ProductUpdatedEvent(
                id, command.Name, command.Description, command.Price, command.Stock, occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(id, nameof(Product), productUpdatedEvent);

            Apply(product, productUpdatedEvent);

            await _context.SaveChangesAsync();

            _eventPublisher.Publish(productUpdatedEvent);
            _logger.LogInformation("[Catalog Service] Product {ProductId} bijgewerkt en event 'ProductUpdatedEvent' gepubliceerd.", id);

            return Ok(product);
        }

        // ---------------------------------------------------------------
        // CQRS - QUERY zijde (lezen vanuit het geprojecteerde read model)
        // ---------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products.AsNoTracking().ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // ---------------------------------------------------------------
        // Event Sourcing: projectie-functies (event -> read model state)
        // ---------------------------------------------------------------

        private static Product Apply(Product product, ProductAddedEvent e)
        {
            product.Id = e.ProductId;
            product.Name = e.Name;
            product.Description = e.Description;
            product.Price = e.Price;
            product.Stock = e.Stock;
            product.SupplierId = e.SupplierId;
            product.CreatedAt = e.OccurredAt;
            product.UpdatedAt = e.OccurredAt;
            return product;
        }

        private static Product Apply(Product product, ProductUpdatedEvent e)
        {
            product.Name = e.Name;
            product.Description = e.Description;
            product.Price = e.Price;
            product.Stock = e.Stock;
            product.UpdatedAt = e.OccurredAt;
            return product;
        }
    }
}
