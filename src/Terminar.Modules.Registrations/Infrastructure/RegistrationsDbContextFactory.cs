using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Terminar.Modules.Registrations.Infrastructure;

public sealed class RegistrationsDbContextFactory : IDesignTimeDbContextFactory<RegistrationsDbContext>
{
    public RegistrationsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<RegistrationsDbContext>()
            .UseNpgsql("Host=localhost;Database=terminar;Username=postgres;Password=postgres")
            .Options;

        return new RegistrationsDbContext(opts);
    }
}
