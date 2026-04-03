using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Tenants.Application.CustomFields;

namespace Terminar.Api.Modules;

public static class CustomFieldsModule
{
    public static IEndpointRouteBuilder MapCustomFieldsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings/custom-fields")
            .RequireAuthorization("StaffOrAdmin")
            .WithTags("CustomFields");

        // GET /api/v1/settings/custom-fields
        group.MapGet("/", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new ListCustomFieldDefinitionsQuery(tenantId.Value), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/settings/custom-fields
        group.MapPost("/", async (
            [FromBody] CreateCustomFieldRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var id = await mediator.Send(
                new CreateCustomFieldDefinitionCommand(tenantId.Value, req.Name, req.FieldType, req.AllowedValues ?? []), ct);
            return Results.Created($"/api/v1/settings/custom-fields/{id}", new { id });
        });

        // PATCH /api/v1/settings/custom-fields/{fieldId}
        group.MapPatch("/{fieldId:guid}", async (
            Guid fieldId,
            [FromBody] UpdateCustomFieldRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(
                new UpdateCustomFieldDefinitionCommand(fieldId, tenantId.Value, req.Name, req.AllowedValues), ct);
            return Results.NoContent();
        });

        // DELETE /api/v1/settings/custom-fields/{fieldId}
        group.MapDelete("/{fieldId:guid}", async (
            Guid fieldId,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new DeleteCustomFieldDefinitionCommand(fieldId, tenantId.Value), ct);
            return Results.NoContent();
        });

        return app;
    }
}

public sealed record CreateCustomFieldRequest(
    string Name,
    string FieldType,
    List<string>? AllowedValues);

public sealed record UpdateCustomFieldRequest(
    string? Name,
    List<string>? AllowedValues);
