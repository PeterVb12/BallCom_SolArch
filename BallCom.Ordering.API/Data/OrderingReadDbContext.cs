using BallCom.Ordering.API.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Data
{
    // LEESKANT (Q). Alleen gedenormaliseerde read models. Logisch gescheiden van
    // de schrijfkant (aparte DbContext, eigen tabellen). Zou fysiek ook een
    // andere database mogen zijn - de projector is de enige schrijver hier.
    public class OrderingReadDbContext : DbContext
    {
        public OrderingReadDbContext(DbContextOptions<OrderingReadDbContext> options) : base(options) { }

        public DbSet<OrderSummary> OrderSummaries { get; set; }
        public DbSet<OrderLineView> OrderLineViews { get; set; }
        public DbSet<CustomerOrderStat> CustomerOrderStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderSummary>().HasKey(x => x.OrderId);
            modelBuilder.Entity<OrderSummary>().Property(x => x.OrderId).ValueGeneratedNever();

            modelBuilder.Entity<OrderLineView>().HasKey(x => x.Id);

            modelBuilder.Entity<CustomerOrderStat>().HasKey(x => x.CustomerEmail);
            modelBuilder.Entity<CustomerOrderStat>().Property(x => x.CustomerEmail).ValueGeneratedNever();
        }
    }
}
