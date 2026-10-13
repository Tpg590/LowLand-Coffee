using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Notification.API.Model;

public partial class CoffeeNotificationDbContext : DbContext
{
    public CoffeeNotificationDbContext()
    {
    }

    public CoffeeNotificationDbContext(DbContextOptions<CoffeeNotificationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<NotifNotification> NotifNotifications { get; set; }

    public virtual DbSet<NotifNotificationLog> NotifNotificationLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=UTA\\SQLEXPRESS;Database=Coffee_NotificationDB;User Id=sa;Password=123;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotifNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__notif_no__3213E83FBA5BC2E7");

            entity.ToTable("notif_notifications");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<NotifNotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__notif_no__3213E83F0A1F80E4");

            entity.ToTable("notif_notification_logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Channel)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("channel");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Notification).WithMany(p => p.NotifNotificationLogs)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_notif_notification_logs_notifications");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
