using Microsoft.EntityFrameworkCore;
using Product.API.Entities;

namespace Product.API.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Entities.Product> Products => Set<Entities.Product>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("product_categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.DisplayOrder).HasDefaultValue(0);
            entity.Property(c => c.IsActive).HasDefaultValue(true);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Entities.Product>(entity =>
        {
            entity.ToTable("product_products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.BasePrice).HasPrecision(12, 2);
            entity.Property(p => p.IsAvailable).HasDefaultValue(true);
            entity.Property(p => p.IsDeleted).HasDefaultValue(false);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductOption>(entity =>
        {
            entity.ToTable("product_options");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OptionType).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Name).IsRequired().HasMaxLength(50);
            entity.Property(o => o.ExtraPrice).HasPrecision(12, 2).HasDefaultValue(0.00m);
            entity.Property(o => o.IsAvailable).HasDefaultValue(true);

            entity.HasOne(o => o.Product)
                  .WithMany(p => p.Options)
                  .HasForeignKey(o => o.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
