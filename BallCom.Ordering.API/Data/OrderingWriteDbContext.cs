using BallCom.Ordering.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Data
{
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
                e.HasIndex(x => new { x.StreamId, x.Version }).IsUnique();
            });
        }
    }
}
