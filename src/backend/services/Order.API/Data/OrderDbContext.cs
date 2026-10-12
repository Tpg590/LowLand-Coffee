using Microsoft.EntityFrameworkCore;
using Order.API.Entities;

namespace Order.API.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.Order> Orders => Set<Entities.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entities.Order>(entity =>
        {
            entity.ToTable("order_orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.UserId).IsRequired();
            entity.Property(o => o.OrderCode).IsRequired().HasMaxLength(20);
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.HasIndex(o => o.UserId);
            entity.Property(o => o.TotalAmount).HasPrecision(12, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(12, 2).HasDefaultValue(0.00m);
            entity.Property(o => o.FinalAmount).HasPrecision(12, 2);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(30);
            entity.Property(o => o.Note).IsUnicode(true);
            entity.Property(o => o.CancelReason).IsUnicode(true);
            entity.Property(o => o.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(o => o.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasMany(o => o.OrderItems)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.OrderPayments)
                  .WithOne(p => p.Order)
                  .HasForeignKey(p => p.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductName).IsRequired().HasMaxLength(150).IsUnicode(true);
            entity.Property(i => i.OptionsSummary).IsUnicode(true);
            entity.Property(i => i.UnitPrice).HasPrecision(12, 2);
            entity.Property(i => i.Subtotal).HasPrecision(12, 2);
        });

        modelBuilder.Entity<OrderPayment>(entity =>
        {
            entity.ToTable("order_payments");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(30);
            entity.Property(p => p.TransactionCode).HasMaxLength(100);
            entity.Property(p => p.Amount).HasPrecision(12, 2);
            entity.Property(p => p.PaymentStatus).IsRequired().HasMaxLength(30);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
        });
    }
}
