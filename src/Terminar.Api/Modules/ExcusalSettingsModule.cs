using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Tenants.Application.Commands.CreateExcusalValidityWindow;
using Terminar.Modules.Tenants.Application.Commands.DeleteExcusalValidityWindow;
using Terminar.Modules.Tenants.Application.Commands.UpdateExcusalValidityWindow;
using Terminar.Modules.Tenants.Application.Commands.UpdateTenantExcusalSettings;
using Terminar.Modules.Tenants.Application.Queries.GetTenantExcusalSettings;
using Terminar.Modules.Tenants.Application.Queries.ListExcusalValidityWindows;

namespace Terminar.Api.Modules;

public static class ExcusalSettingsModule
{
    public static IEndpointRouteBuilder MapExcusalSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/settings/excusal-policy
        app.MapGet("/api/v1/settings/excusal-policy", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new GetTenantExcusalSettingsQuery(tenantId.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("StaffOrAdmin").WithTags("ExcusalSettings");

        // PATCH /api/v1/settings/excusal-policy
        app.MapPatch("/api/v1/settings/excusal-policy", async (
            [FromBody] UpdateExcusalPolicyRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UpdateTenantExcusalSettingsCommand(
                tenantId.Value, req.CreditGenerationEnabled, req.ForwardWindowCount,
                req.UnenrollmentDeadlineDays, req.ExcusalDeadlineHours), ct);
            return Results.Ok();
        }).RequireAuthorization("AdminOnly").WithTags("ExcusalSettings");

        // GET /api/v1/settings/excusal-windows
        app.MapGet("/api/v1/settings/excusal-windows", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new ListExcusalValidityWindowsQuery(tenantId.Value), ct);
            return Results.Ok(result);
        }).RequireAuthorization("StaffOrAdmin").WithTags("ExcusalSettings");

        // POST /api/v1/settings/excusal-windows
        app.MapPost("/api/v1/settings/excusal-windows", async (
            [FromBody] CreateWindowRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var id = await mediator.Send(new CreateExcusalValidityWindowCommand(tenantId.Value, req.Name, req.StartDate, req.EndDate), ct);
            return Results.Created($"/api/v1/settings/excusal-windows/{id}", new { window_id = id });
        }).RequireAuthorization("AdminOnly").WithTags("ExcusalSettings");

        // PATCH /api/v1/settings/excusal-windows/{id}
        app.MapPatch("/api/v1/settings/excusal-windows/{id:guid}", async (
            Guid id,
            [FromBody] UpdateWindowRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UpdateExcusalValidityWindowCommand(id, tenantId.Value, req.Name, req.StartDate, req.EndDate), ct);
            return Results.Ok();
        }).RequireAuthorization("AdminOnly").WithTags("ExcusalSettings");

        // DELETE /api/v1/settings/excusal-windows/{id}
        app.MapDelete("/api/v1/settings/excusal-windows/{id:guid}", async (
            Guid id,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new DeleteExcusalValidityWindowCommand(id, tenantId.Value), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithTags("ExcusalSettings");

        return app;
    }
}

public sealed record UpdateExcusalPolicyRequest(bool? CreditGenerationEnabled, int? ForwardWindowCount, int? UnenrollmentDeadlineDays, int? ExcusalDeadlineHours);
public sealed record CreateWindowRequest(string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record UpdateWindowRequest(string? Name, DateOnly? StartDate, DateOnly? EndDate);
