using BallCom.Payment.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Payment.API.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<StoredEvent> EventStore { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredEvent>().HasKey(e => e.Sequence);
            modelBuilder.Entity<StoredEvent>()
                        .Property(e => e.Sequence)
                        .ValueGeneratedOnAdd();

            modelBuilder.Entity<Transaction>()
                        .HasIndex(t => t.OrderId)
                        .IsUnique();
        }
    }
}
