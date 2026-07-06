using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BallCom.Catalog.API.Commands
{
    public class ReplayProductsCommandHandler
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<ReplayProductsCommandHandler> _logger;

        public ReplayProductsCommandHandler(CatalogDbContext context, ILogger<ReplayProductsCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task HandleAsync()
        {
            _logger.LogWarning("[Catalog Service - ES] START REPLAY: Het read model wordt volledig opnieuw opgebouwd vanuit de Event Store.");

            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Products\";");
            await _context.SaveChangesAsync();

            var allEvents = await _context.EventStore
                .OrderBy(e => e.Sequence)
                .ToListAsync();

            _logger.LogInformation("[Catalog Service - ES] {Count} events gevonden in de Event Store om te verwerken.", allEvents.Count);

            var reconstructedProducts = new List<Product>();

            foreach (var eventEntry in allEvents)
            {
                if (eventEntry.EventType == nameof(ProductAddedEvent))
                {
                    var @event = JsonSerializer.Deserialize<ProductAddedEvent>(eventEntry.Payload);
                    if (@event != null)
                    {
                        var newProduct = new Product();
                        Apply(newProduct, @event);
                        reconstructedProducts.Add(newProduct);
                    }
                }
                else if (eventEntry.EventType == nameof(ProductUpdatedEvent))
                {
                    var @event = JsonSerializer.Deserialize<ProductUpdatedEvent>(eventEntry.Payload);
                    if (@event != null)
                    {
                        var existingProduct = reconstructedProducts.FirstOrDefault(p => p.Id == @event.ProductId);
                        if (existingProduct != null)
                        {
                            Apply(existingProduct, @event);
                        }
                    }
                }
            }

            if (reconstructedProducts.Any())
            {
                _context.Products.AddRange(reconstructedProducts);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("[Catalog Service - ES] REPLAY SUCCESVOL: {Count} producten zijn succesvol hersteld.", reconstructedProducts.Count);
        }

        private void Apply(Product product, ProductAddedEvent e)
        {
            product.Id = e.ProductId;
            product.Name = e.Name;
            product.Description = e.Description;
            product.Price = e.Price;
            product.Stock = e.Stock;
            product.SupplierId = e.SupplierId;
            product.CreatedAt = e.OccurredAt;
            product.UpdatedAt = e.OccurredAt;
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