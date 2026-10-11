using Microsoft.EntityFrameworkCore;
using Auth.API.Entities;

namespace Auth.API.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("auth_roles");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("NEWID()");
            entity.Property(r => r.Name).HasColumnName("name").IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Description).HasColumnName("description");
            entity.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        });

        // 2. Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("auth_permissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("NEWID()");
            entity.Property(p => p.Code).HasColumnName("code").IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).HasColumnName("description");
        });

        // 3. RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("auth_role_permissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.Property(rp => rp.RoleId).HasColumnName("role_id");
            entity.Property(rp => rp.PermissionId).HasColumnName("permission_id");

            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("auth_users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("NEWID()");
            entity.Property(u => u.RoleId).HasColumnName("role_id").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
            entity.Property(u => u.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(100);
            entity.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
            entity.Property(u => u.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(u => u.Address).HasColumnName("address");
            entity.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
            entity.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("GETDATE()");

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
