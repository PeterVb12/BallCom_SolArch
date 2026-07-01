using BallCom.ServiceDepartment.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BallCom.ServiceDepartment.API.Data
{
    public class ServiceDepartmentDbContext : DbContext
    {
        public ServiceDepartmentDbContext(DbContextOptions<ServiceDepartmentDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<OrderAuditEntry> OrderAuditEntries { get; set; }
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
