using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Terminar.Modules.Tenants.Infrastructure;

public sealed class TenantsDbContextFactory : IDesignTimeDbContextFactory<TenantsDbContext>
{
    public TenantsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<TenantsDbContext>()
            .UseNpgsql("Host=localhost;Database=terminar;Username=postgres;Password=postgres")
            .Options;

        return new TenantsDbContext(opts, null!);
    }
}
