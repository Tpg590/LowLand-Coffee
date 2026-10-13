using System;
using System.Collections.Generic;
using Engagement.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engagement.API.Data;

public partial class EngagementDbContext : DbContext
{
    public EngagementDbContext()
    {
    }

    public EngagementDbContext(DbContextOptions<EngagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<LoyaltyMembers> LoyaltyMembers { get; set; }

    public virtual DbSet<LoyaltyPointTransactions> LoyaltyPointTransactions { get; set; }

    public virtual DbSet<LoyaltyTiers> LoyaltyTiers { get; set; }

    public virtual DbSet<PromoVoucherUsages> PromoVoucherUsages { get; set; }

    public virtual DbSet<PromoVouchers> PromoVouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost,1434;Database=Coffee_EngagementDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoyaltyMembers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__loyalty___3213E83F0D2C7F9E");

            entity.ToTable("loyalty_members");

            entity.HasIndex(e => e.UserId, "UQ__loyalty___B9BE370ED80CAD3E").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentPoints).HasColumnName("current_points");
            entity.Property(e => e.TierId).HasColumnName("tier_id");
            entity.Property(e => e.TotalAccumulatedPoints).HasColumnName("total_accumulated_points");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Tier).WithMany(p => p.LoyaltyMembers)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_loyalty_members_tiers");
        });

        modelBuilder.Entity<LoyaltyPointTransactions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__loyalty___3213E83F0FB1ED7F");

            entity.ToTable("loyalty_point_transactions");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PointsChanged).HasColumnName("points_changed");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("transaction_type");

            entity.HasOne(d => d.Member).WithMany(p => p.LoyaltyPointTransactions)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_loyalty_point_transactions_members");
        });

        modelBuilder.Entity<LoyaltyTiers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__loyalty___3213E83F3D49CB93");

            entity.ToTable("loyalty_tiers");

            entity.HasIndex(e => e.TierCode, "UQ__loyalty___77A47EA64D4B1BC5").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BenefitsDescription).HasColumnName("benefits_description");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percent");
            entity.Property(e => e.MinPoints).HasColumnName("min_points");
            entity.Property(e => e.PointMultiplier)
                .HasDefaultValue(1.00m)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("point_multiplier");
            entity.Property(e => e.TierCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("tier_code");
            entity.Property(e => e.TierName)
                .HasMaxLength(50)
                .HasColumnName("tier_name");
        });

        modelBuilder.Entity<PromoVoucherUsages>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__promo_vo__3213E83F9DB19185");

            entity.ToTable("promo_voucher_usages");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Voucher).WithMany(p => p.PromoVoucherUsages)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_promo_voucher_usages_vouchers");
        });

        modelBuilder.Entity<PromoVouchers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__promo_vo__3213E83F20EE870E");

            entity.ToTable("promo_vouchers");

            entity.HasIndex(e => e.Code, "UQ__promo_vo__357D4CF9A6EC0A32").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("discount_value");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinOrderValue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("min_order_value");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UsedQuantity).HasColumnName("used_quantity");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
