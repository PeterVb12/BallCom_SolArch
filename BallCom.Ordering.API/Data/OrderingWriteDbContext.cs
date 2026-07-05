using BallCom.Ordering.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Data
{
    // SCHRIJFKANT (C). Bevat UITSLUITEND de append-only event store (bron van
    // waarheid) plus een read-only referentietabel met producten die via
    // RabbitMQ vanuit de Catalog-service binnenkomt (nodig voor prijs/validatie).
    // Er is hier bewust GEEN "Orders"-statustabel: de orderstaat leeft in de events.
    public class OrderingWriteDbContext : DbContext
    {
        public OrderingWriteDbContext(DbContextOptions<OrderingWriteDbContext> options) : base(options) { }

        public DbSet<StoredEvent> OrderEvents { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredEvent>(e =>
            {
                e.ToTable("OrderEvents");
                e.HasKey(x => x.Sequence);
                e.Property(x => x.Sequence).ValueGeneratedOnAdd();
                // Optimistic concurrency: binnen één stream is elke versie uniek.
                e.HasIndex(x => new { x.StreamId, x.Version }).IsUnique();
            });
        }
    }
}
