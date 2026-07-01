using BallCom.Logistics.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Logistics.API.Data
{
    public class LogisticsDbContext : DbContext
    {
        public LogisticsDbContext(DbContextOptions<LogisticsDbContext> options) : base(options) { }

        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<StoredEvent> EventStore { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredEvent>().HasKey(e => e.Sequence);
            modelBuilder.Entity<StoredEvent>()
                        .Property(e => e.Sequence)
                        .ValueGeneratedOnAdd();

            modelBuilder.Entity<Shipment>()
                        .HasIndex(s => s.OrderId)
                        .IsUnique();
        }
    }
}
