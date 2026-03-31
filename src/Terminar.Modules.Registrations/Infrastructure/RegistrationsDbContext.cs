using Microsoft.EntityFrameworkCore;

namespace Terminar.Modules.Registrations.Infrastructure;

public sealed class RegistrationsDbContext(DbContextOptions<RegistrationsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("registrations");
    }
}
