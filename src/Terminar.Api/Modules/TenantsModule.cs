using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Modules.Tenants.Application.Commands.CreateTenant;
using Terminar.Modules.Tenants.Application.Queries.GetTenant;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Modules;

public static class TenantsModule
{
    public static IEndpointRouteBuilder MapTenantsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .RequireAuthorization("SystemAdminOnly")
            .WithTags("Tenants");

        group.MapPost("/", async (
            [FromBody] CreateTenantRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateTenantCommand(req.Name, req.Slug, req.DefaultLanguageCode), ct);
            return Results.Created($"/api/tenants/{result.TenantId}", result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTenantQuery(TenantId.From(id)), ct);
            return Results.Ok(result);
        });

        return app;
    }
}

public sealed record CreateTenantRequest(string Name, string Slug, string DefaultLanguageCode);
