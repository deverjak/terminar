using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Infrastructure;

public sealed class RegistrationsDbContext(DbContextOptions<RegistrationsDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("registrations");

        modelBuilder.Entity<Registration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId)
                .HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.ParticipantEmail)
                .HasConversion(v => v.Value, v => Email.From(v))
                .HasMaxLength(254);
            e.Property(x => x.ParticipantName).HasMaxLength(200).IsRequired();
            e.Property(x => x.RegistrationSource).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.CourseId);

            e.Ignore(x => x.DomainEvents);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        var events = ChangeTracker.Entries<Registration>()
            .SelectMany(e => e.Entity.ClearDomainEvents())
            .ToList();

        foreach (var domainEvent in events)
            await mediator.Publish(domainEvent, cancellationToken);

        return result;
    }
}
