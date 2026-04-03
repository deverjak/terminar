using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure.Repositories;


namespace Terminar.Modules.Tenants.Infrastructure;

public static class TenantsModule
{
    public static IServiceCollection AddTenantsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<TenantsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IExcusalValidityWindowRepository, ExcusalValidityWindowRepository>();
        services.AddScoped<ICustomFieldDefinitionRepository, CustomFieldDefinitionRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TenantsModule).Assembly));

        services.AddValidatorsFromAssembly(typeof(TenantsModule).Assembly);

        return services;
    }
}
