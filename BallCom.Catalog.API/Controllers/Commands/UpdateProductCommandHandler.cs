using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Models;
using BallCom.Catalog.API.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Commands
{
    public class UpdateProductCommandHandler
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<UpdateProductCommandHandler> _logger;
        private readonly IEventPublisher _eventPublisher;

        public UpdateProductCommandHandler(
            CatalogDbContext context, 
            ILogger<UpdateProductCommandHandler> logger,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        // We geven naast het command ook het Id mee, omdat die uit de URL van de controller komt
        public async Task<Product?> HandleAsync(Guid id, UpdateProductCommand command)
        {
            //Zoek het bestaande product op in de database (Write model heeft wél tracking nodig!)
            var product = await _context.Products.FindAsync(id);
            if (product is null)
            {
                return null; // Controller zet dit om in een NotFound
            }

            // Business Rule: Validatie van invoer
            if (string.IsNullOrWhiteSpace(command.Name) || command.Price <= 0)
            {
                throw new ArgumentException("Een product vereist minimaal een naam en een prijs > 0.");
            }

            var occurredAt = DateTime.UtcNow;
            
            // Maak het Event aan
            var productUpdatedEvent = new ProductUpdatedEvent(
                id, command.Name, command.Description, command.Price, command.Stock, occurredAt);

            // Event Sourcing: Sla de mutatie op in de event store
            var eventStore = new EventStore(_context);
            eventStore.Append(id, nameof(Product), productUpdatedEvent);

            // Projecteer de wijziging naar het Read Model (de Products tabel)
            Apply(product, productUpdatedEvent);

            // Opslaan in de database
            await _context.SaveChangesAsync();

            // EDA: Publiceer naar de Message Broker (RabbitMQ)
            _eventPublisher.Publish(productUpdatedEvent);
            
            _logger.LogInformation("[Catalog Service - CQRS] Product {ProductId} bijgewerkt en ProductUpdatedEvent gepubliceerd.", id);

            return product;
        }

        // De specifieke projectie-functie voor updates
        private void Apply(Product product, ProductUpdatedEvent e)
        {
            product.Name = e.Name;
            product.Description = e.Description;
            product.Price = e.Price;
            product.Stock = e.Stock;
            product.UpdatedAt = e.OccurredAt;
        }
    }
}