using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Terminar.Modules.Registrations.Infrastructure;

public static class RegistrationsModule
{
    public static IServiceCollection AddRegistrationsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<RegistrationsDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        return services;
    }
}
