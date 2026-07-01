using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Models;
using BallCom.Catalog.API.Messaging; // Zorg dat dit matcht met jouw IEventPublisher namespace
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Commands
{
    public class AddProductCommandHandler
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<AddProductCommandHandler> _logger;
        private readonly IEventPublisher _eventPublisher; // Vervang door jouw exacte type/interface indien anders

        public AddProductCommandHandler(
            CatalogDbContext context, 
            ILogger<AddProductCommandHandler> logger,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<Product> HandleAsync(AddProductCommand command)
        {
            // 1. DDD Business Rule: Validatie van invoer
            if (string.IsNullOrWhiteSpace(command.Name) || command.Price <= 0 || command.SupplierId == Guid.Empty)
            {
                throw new ArgumentException("Een product vereist minimaal een naam, een prijs > 0 en een supplierId.");
            }

            // 2. DDD Business Rule: Alleen vertrouwde suppliers
            var supplierIsTrusted = await _context.Suppliers.AnyAsync(s => s.Id == command.SupplierId);
            if (!supplierIsTrusted)
            {
                throw new InvalidOperationException("Alleen vertrouwde suppliers mogen producten toevoegen aan de catalogus.");
            }

            // 3. Controleer of deze leverancier dit product al eens heeft toegevoegd (Voorraad-check)
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == command.Name.ToLower() && p.SupplierId == command.SupplierId);

            if (existingProduct != null)
            {
                existingProduct.Stock += command.Stock;
                
                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[Catalog Service - CQRS] Product '{Name}' bestond al. Voorraad opgehoogd met {Stock}.", 
                    existingProduct.Name, command.Stock);

                // We returnen het geüpdatete product naar de controller
                return existingProduct;
            }

            // 4. Nieuw product aanmaken via Event Sourcing & CQRS
            var productId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var productAddedEvent = new ProductAddedEvent(
                productId, command.Name, command.Description, command.Price, command.Stock, command.SupplierId, occurredAt);

            // Event Sourcing: Sla het feit op in de event store
            var eventStore = new EventStore(_context);
            eventStore.Append(productId, nameof(Product), productAddedEvent);

            // CQRS: Projecteer het Read Model (de Products tabel) vanuit dat event
            var product = Apply(new Product(), productAddedEvent);
            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            // Event-Driven Architecture: Publiceer naar RabbitMQ voor downstream services (Ordering)
            _eventPublisher.Publish(productAddedEvent);
            
            _logger.LogInformation("[Catalog Service - CQRS] Nieuw product {ProductId} toegevoegd en ProductAddedEvent gepubliceerd.", productId);

            return product;
        }

        // De hulpmethode voor Event Sourcing projectie (CQRS)
        private Product Apply(Product product, ProductAddedEvent @event)
        {
            product.Id = @event.ProductId;
            product.Name = @event.Name;
            product.Description = @event.Description;
            product.Price = @event.Price;
            product.Stock = @event.Stock;
            product.SupplierId = @event.SupplierId;
            return product;
        }
    }
}