using Microsoft.EntityFrameworkCore;

namespace Terminar.Api.Infrastructure;

public sealed class DatabaseMigrationService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying database migrations...");

        await using var scope = serviceProvider.CreateAsyncScope();

        await MigrateAsync<Terminar.Modules.Tenants.Infrastructure.TenantsDbContext>(scope, cancellationToken);
        await MigrateAsync<Terminar.Modules.Identity.Infrastructure.Identity.AppIdentityDbContext>(scope, cancellationToken);
        await MigrateAsync<Terminar.Modules.Courses.Infrastructure.CoursesDbContext>(scope, cancellationToken);
        await MigrateAsync<Terminar.Modules.Registrations.Infrastructure.RegistrationsDbContext>(scope, cancellationToken);

        logger.LogInformation("Database migrations applied successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task MigrateAsync<TContext>(AsyncServiceScope scope, CancellationToken cancellationToken)
        where TContext : DbContext
    {
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations applied for {Context}", typeof(TContext).Name);
    }
}
