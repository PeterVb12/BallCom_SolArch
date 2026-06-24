using BallCom.Catalog.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Catalog.API.Data
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

        // Read models (projecties vanuit de event store)
        public DbSet<Product> Products { get; set; }
        public DbSet<TrustedSupplier> Suppliers { get; set; }

        // Event Sourcing - append-only log van alle mutaties
        public DbSet<StoredEvent> EventStore { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredEvent>().HasKey(e => e.Sequence);
            modelBuilder.Entity<StoredEvent>()
                        .Property(e => e.Sequence)
                        .ValueGeneratedOnAdd();
        }
    }
}
