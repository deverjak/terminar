using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Terminar.Modules.Identity.Infrastructure.Identity;

public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<AppIdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<AppIdentityUser>(entity =>
        {
            entity.Property(u => u.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(u => u.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
            entity.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(u => new { u.TenantId, u.UserName }).IsUnique();
            entity.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        });
    }
}
