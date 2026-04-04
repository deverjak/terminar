using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Tenants.Domain;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Infrastructure;

public sealed class TenantsDbContext(DbContextOptions<TenantsDbContext> options, IMediator mediator)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ExcusalValidityWindow> ExcusalValidityWindows => Set<ExcusalValidityWindow>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<TenantPluginActivation> TenantPluginActivations => Set<TenantPluginActivation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tenants");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id)
                .HasConversion(id => id.Value, value => TenantId.From(value))
                .HasColumnName("tenant_id");
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(t => t.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            entity.HasIndex(t => t.Slug).IsUnique();
            entity.Property(t => t.DefaultLanguageCode).HasColumnName("default_language_code").HasMaxLength(5).IsRequired();
            entity.Property(t => t.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Ignore(t => t.DomainEvents);
            entity.OwnsOne(t => t.ExcusalSettings, s =>
            {
                s.Property(x => x.CreditGenerationEnabled).HasColumnName("excusal_credit_generation_enabled").HasDefaultValue(false);
                s.Property(x => x.ForwardWindowCount).HasColumnName("excusal_forward_window_count").HasDefaultValue(2);
                s.Property(x => x.UnenrollmentDeadlineDays).HasColumnName("excusal_unenrollment_deadline_days").HasDefaultValue(14);
                s.Property(x => x.ExcusalDeadlineHours).HasColumnName("excusal_deadline_hours").HasDefaultValue(24);
            });
        });

        modelBuilder.Entity<CustomFieldDefinition>(e =>
        {
            e.ToTable("custom_field_definitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId)
                .HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.AllowedValues).HasColumnType("text[]");
            e.Property(x => x.DisplayOrder).HasDefaultValue(0);
            e.Property(x => x.CreatedAt);
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<TenantPluginActivation>(e =>
        {
            e.ToTable("tenant_plugin_activations");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId)
                .HasConversion(v => v.Value, v => TenantId.From(v))
                .HasColumnName("tenant_id");
            e.Property(x => x.PluginId).HasColumnName("plugin_id").HasMaxLength(64).IsRequired();
            e.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
            e.Property(x => x.EnabledAt).HasColumnName("enabled_at");
            e.Property(x => x.DisabledAt).HasColumnName("disabled_at");
            e.HasIndex(x => new { x.TenantId, x.PluginId }).IsUnique();
            e.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<ExcusalValidityWindow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique().HasFilter("deleted_at IS NULL");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasQueryFilter(x => x.DeletedAt == null);
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(cancellationToken);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var tenantEvents = ChangeTracker.Entries<Terminar.SharedKernel.AggregateRoot<TenantId>>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .SelectMany(e => e.Entity.ClearDomainEvents())
            .ToList();
        var guidEvents = ChangeTracker.Entries<Terminar.SharedKernel.AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .SelectMany(e => e.Entity.ClearDomainEvents())
            .ToList();
        foreach (var @event in tenantEvents.Concat(guidEvents))
            await mediator.Publish(@event, cancellationToken);
    }
}
