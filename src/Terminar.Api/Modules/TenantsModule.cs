using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Modules.Identity.Application.Commands.CreateStaffUser;
using Terminar.Modules.Tenants.Application.Commands.CreateTenant;
using Terminar.Modules.Tenants.Application.Queries.GetTenant;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Modules;

public static class TenantsModule
{
    public static IEndpointRouteBuilder MapTenantsEndpoints(this IEndpointRouteBuilder app)
    {
        // POST is intentionally unauthenticated — bootstrap operation, no tenant exists yet.
        // Protect via network policy / firewall in production.
        app.MapPost("/api/v1/tenants", async (
            [FromBody] CreateTenantRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenant = await mediator.Send(
                new CreateTenantCommand(req.Name, req.Slug, req.DefaultLanguageCode), ct);

            await mediator.Send(new CreateStaffUserCommand(
                TenantId.From(tenant.TenantId),
                req.AdminUsername,
                req.AdminEmail,
                req.AdminPassword,
                "Admin"), ct);

            return Results.Created($"/api/v1/tenants/{tenant.TenantId}", new
            {
                tenant_id = tenant.TenantId,
                name = tenant.Name,
                slug = tenant.Slug,
                status = "Active",
                created_at = tenant.CreatedAt
            });
        }).AllowAnonymous().WithTags("Tenants");

        app.MapGet("/api/v1/tenants/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTenantQuery(TenantId.From(id)), ct);
            return Results.Ok(result);
        }).RequireAuthorization("SystemAdminOnly").WithTags("Tenants");

        return app;
    }
}

public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string DefaultLanguageCode,
    string AdminUsername,
    string AdminEmail,
    string AdminPassword);
