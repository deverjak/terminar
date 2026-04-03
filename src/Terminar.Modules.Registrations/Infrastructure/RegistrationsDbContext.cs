using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Infrastructure;

public sealed class RegistrationsDbContext(DbContextOptions<RegistrationsDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<ParticipantMagicLink> ParticipantMagicLinks => Set<ParticipantMagicLink>();
    public DbSet<Excusal> Excusals => Set<Excusal>();
    public DbSet<ExcusalCredit> ExcusalCredits => Set<ExcusalCredit>();

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

        modelBuilder.Entity<ParticipantMagicLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.ParticipantEmail).HasConversion(v => v.Value, v => Email.From(v)).HasMaxLength(254);
            e.Property(x => x.MagicLinkToken).HasMaxLength(64).IsRequired();
            e.Property(x => x.PortalToken).HasMaxLength(64);
            e.HasIndex(x => new { x.TenantId, x.MagicLinkToken }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.PortalToken }).IsUnique().HasFilter("\"PortalToken\" IS NOT NULL");
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.IsMagicLinkValid);
            e.Ignore(x => x.IsPortalTokenValid);
        });

        modelBuilder.Entity<Excusal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.ParticipantEmail).HasMaxLength(254).IsRequired();
            e.Property(x => x.ParticipantName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.RegistrationId, x.SessionId }).IsUnique();
            e.HasIndex(x => x.CourseId);
            e.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<ExcusalCredit>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.ParticipantEmail).HasMaxLength(254).IsRequired();
            e.Property(x => x.ParticipantName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Tags)
                .HasColumnType("text[]");
            e.Property(x => x.ValidWindowIds)
                .HasColumnType("uuid[]");
            e.OwnsMany(x => x.AuditEntries, a =>
            {
                a.WithOwner().HasForeignKey(x => x.ExcusalCreditId);
                a.HasKey(x => x.Id);
                a.ToTable("ExcusalCreditAuditEntries");
                a.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(50);
                a.Property(x => x.FieldChanged).HasMaxLength(100);
                a.Property(x => x.PreviousValue).HasColumnType("text");
                a.Property(x => x.NewValue).HasColumnType("text");
            });
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.IsActive);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // EF Core may classify newly added owned ExcusalCreditAuditEntry entities as Modified
        // (non-zero Guid key, no prior snapshot) instead of Added. Audit entries are immutable,
        // so Modified state always means a newly detected entry that should be inserted.
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<ExcusalCreditAuditEntry>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        var events = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .SelectMany(e => e.Entity.ClearDomainEvents())
            .ToList();

        foreach (var domainEvent in events)
            await mediator.Publish(domainEvent, cancellationToken);

        return result;
    }
}
