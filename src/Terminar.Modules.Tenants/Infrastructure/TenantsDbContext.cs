using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Tenants.Domain;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Infrastructure;

public sealed class TenantsDbContext(DbContextOptions<TenantsDbContext> options, IMediator mediator)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

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
        var aggregates = ChangeTracker.Entries<Terminar.SharedKernel.AggregateRoot<TenantId>>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.ClearDomainEvents()).ToList();

        foreach (var @event in events)
            await mediator.Publish(@event, cancellationToken);
    }
}
