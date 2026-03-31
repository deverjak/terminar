using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Terminar.Modules.Courses.Application.Ports;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Registrations.Domain.Services;
using Terminar.Modules.Registrations.Infrastructure.Repositories;

namespace Terminar.Modules.Registrations.Infrastructure;

public static class RegistrationsModule
{
    public static IServiceCollection AddRegistrationsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<RegistrationsDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<IRegistrationCountReader, RegistrationRepository>();
        services.AddScoped<RegistrationCapacityChecker>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegistrationsModule).Assembly));

        services.AddValidatorsFromAssembly(typeof(RegistrationsModule).Assembly);

        return services;
    }
}
