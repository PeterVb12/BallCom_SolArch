using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Queries
{
    public class CatalogQueryHandler
    {
        private readonly CatalogDbContext _context;

        public CatalogQueryHandler(CatalogDbContext context)
        {
            _context = context;
        }

        // Handler voor de volledige lijst
        public async Task<List<Product>> HandleAsync(GetAllProductsQuery query)
        {
            return await _context.Products
                .AsNoTracking() // Zorgt voor performance optimalisatie bij geen achtergrond taken
                .ToListAsync();
        }

        // Handler voor één specifiek product
        public async Task<Product?> HandleAsync(GetProductByIdQuery query)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == query.Id);
        }
    }
}