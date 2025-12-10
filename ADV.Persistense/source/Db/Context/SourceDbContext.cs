using Microsoft.EntityFrameworkCore;
using ADV.Domain.Entities.DB;

namespace ADV.Persistense.repositorie.Db.Context
{
    public class SourceDbContext : DbContext
    {
        public SourceDbContext(DbContextOptions<SourceDbContext> options) : base(options)
        {
        }

        public DbSet<DbOrders> Orders { get; set; }
        public DbSet<DbOrderDetails> OrderDetails { get; set; }
        public DbSet<DbProducts> Products { get; set; }
        public DbSet<DbCustomers> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbProducts>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(p => p.ProductID);
            });

            modelBuilder.Entity<DbCustomers>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(c => c.CustomerID);
            });

            modelBuilder.Entity<DbOrders>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(o => o.OrderID);

                entity.HasOne<DbCustomers>()
                      .WithMany()
                      .HasForeignKey(o => o.CustomerID);
            });

            modelBuilder.Entity<DbOrderDetails>(entity =>
            {
                entity.ToTable("OrderDetails");

                entity.HasKey(d => new { d.OrderID, d.ProductID });

                entity.HasOne<DbOrders>()
                      .WithMany()
                      .HasForeignKey(d => d.OrderID);

                entity.HasOne<DbProducts>()
                      .WithMany()
                      .HasForeignKey(d => d.ProductID);
            });
        }
    }
}
