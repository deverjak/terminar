using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Domain;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Infrastructure;

public sealed class CoursesDbContext(DbContextOptions<CoursesDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("courses");

        modelBuilder.Entity<Course>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId)
                .HasConversion(v => v.Value, v => TenantId.From(v));
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.CourseType).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.RegistrationMode).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => x.TenantId);

            e.HasMany(x => x.Sessions)
                .WithOne()
                .HasForeignKey("CourseId")
                .OnDelete(DeleteBehavior.Cascade);

            e.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Location).HasMaxLength(500);
            e.Ignore(x => x.EndsAt);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        var events = ChangeTracker.Entries<Course>()
            .SelectMany(e => e.Entity.ClearDomainEvents())
            .ToList();

        foreach (var domainEvent in events)
            await mediator.Publish(domainEvent, cancellationToken);

        return result;
    }
}
