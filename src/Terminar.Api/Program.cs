using FluentValidation;
using Scalar.AspNetCore;
using Terminar.Api.Infrastructure;
using Terminar.Api.Middleware;
using Terminar.Api.Modules;
using Terminar.Api.Notifications;
using Terminar.Api.Pipeline;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Identity.Infrastructure;
using Terminar.Modules.Registrations.Infrastructure;
using Terminar.Modules.Tenants.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Connection string from Aspire service discovery
var connectionString = builder.Configuration.GetConnectionString("terminar-db")
    ?? throw new InvalidOperationException("Connection string 'terminar-db' not found.");

// MediatR pipeline behaviors (registered once, shared across all module assemblies)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Module registrations (each module self-registers its own MediatR handlers and validators)
builder.Services.AddTenantsModule(connectionString);
builder.Services.AddIdentityModule(connectionString, builder.Configuration);
builder.Services.AddCoursesModule(connectionString);
builder.Services.AddRegistrationsModule(connectionString);

// Tenant resolution
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Email notifications (stub)
builder.Services.AddScoped<IEmailNotificationService, StubEmailNotificationService>();

// Background services
builder.Services.AddHostedService<DatabaseMigrationService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantResolutionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

// Route registrations
app.MapTenantsEndpoints();
app.MapIdentityEndpoints();
app.MapCoursesEndpoints();
app.MapRegistrationsEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.Run();
