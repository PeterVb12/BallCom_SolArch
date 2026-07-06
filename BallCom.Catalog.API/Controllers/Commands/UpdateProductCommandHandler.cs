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

        public async Task<Product?> HandleAsync(Guid id, UpdateProductCommand command)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(command.Name) || command.Price <= 0)
            {
                throw new ArgumentException("Een product vereist minimaal een naam en een prijs > 0.");
            }

            var occurredAt = DateTime.UtcNow;
            
            var productUpdatedEvent = new ProductUpdatedEvent(
                id, command.Name, command.Description, command.Price, command.Stock, occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(id, nameof(Product), productUpdatedEvent);

            Apply(product, productUpdatedEvent);

            await _context.SaveChangesAsync();

            _eventPublisher.Publish(productUpdatedEvent);
            
            _logger.LogInformation("[Catalog Service - CQRS] Product {ProductId} bijgewerkt en ProductUpdatedEvent gepubliceerd.", id);

            return product;
        }

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