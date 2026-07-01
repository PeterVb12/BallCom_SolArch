using BallCom.Warehouse.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Warehouse.API.Data
{
    public class WarehouseDbContext : DbContext
    {
        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

        public DbSet<PickList> PickLists { get; set; }
        public DbSet<PickListLine> PickListLines { get; set; }

        public DbSet<StoredEvent> EventStore { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredEvent>().HasKey(e => e.Sequence);
            modelBuilder.Entity<StoredEvent>()
                        .Property(e => e.Sequence)
                        .ValueGeneratedOnAdd();

            modelBuilder.Entity<PickList>()
                        .HasIndex(p => p.OrderId)
                        .IsUnique();

            modelBuilder.Entity<PickList>()
                        .HasMany(p => p.Lines)
                        .WithOne()
                        .HasForeignKey(l => l.PickListId);
        }
    }
}
