using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Models;
using BallCom.Catalog.API.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Commands
{
    public class AddProductCommandHandler
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<AddProductCommandHandler> _logger;
        private readonly IEventPublisher _eventPublisher;

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
            if (string.IsNullOrWhiteSpace(command.Name) || command.Price <= 0 || command.SupplierId == Guid.Empty)
            {
                throw new ArgumentException("Een product vereist minimaal een naam, een prijs > 0 en een supplierId.");
            }

            var supplierIsTrusted = await _context.Suppliers.AnyAsync(s => s.Id == command.SupplierId);
            if (!supplierIsTrusted)
            {
                throw new InvalidOperationException("Alleen vertrouwde suppliers mogen producten toevoegen aan de catalogus.");
            }

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == command.Name.ToLower() && p.SupplierId == command.SupplierId);

            if (existingProduct != null)
            {
                existingProduct.Stock += command.Stock;
                
                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[Catalog Service - CQRS] Product '{Name}' bestond al. Voorraad opgehoogd met {Stock}.", 
                    existingProduct.Name, command.Stock);

                return existingProduct;
            }

            var productId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var productAddedEvent = new ProductAddedEvent(
                productId, command.Name, command.Description, command.Price, command.Stock, command.SupplierId, occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(productId, nameof(Product), productAddedEvent);

            var product = Apply(new Product(), productAddedEvent);
            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            _eventPublisher.Publish(productAddedEvent);
            
            _logger.LogInformation("[Catalog Service - CQRS] Nieuw product {ProductId} toegevoegd en ProductAddedEvent gepubliceerd.", productId);

            return product;
        }

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