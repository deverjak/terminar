using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Registrations.Application.Commands.SoftDeleteExcusalCredit;
using Terminar.Modules.Registrations.Application.Commands.UpdateExcusalCredit;
using Terminar.Modules.Registrations.Application.Queries.GetExcusalCredits;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Tenants.Application.Commands.CreateExcusalValidityWindow;
using Terminar.Modules.Tenants.Application.Commands.DeleteExcusalValidityWindow;
using Terminar.Modules.Tenants.Application.Commands.UpdateExcusalValidityWindow;
using Terminar.Modules.Tenants.Application.Commands.UpdateTenantExcusalSettings;
using Terminar.Modules.Tenants.Application.Queries.GetTenantExcusalSettings;
using Terminar.Modules.Tenants.Application.Queries.ListExcusalValidityWindows;
using Terminar.SharedKernel.Plugins;

namespace Terminar.Api.Plugins;

public sealed class ExcusalPlugin : ITerminarPlugin
{
    public PluginDescriptor Descriptor => new(
        "excusals",
        "Excusals",
        "Allows participants to be excused from courses and receive credits for future enrollment.");

    public void RegisterServices(IServiceCollection services)
    {
        // All excusal DI is already registered by the existing modules
    }

    public void MapEndpoints(IEndpointRouteBuilder app, IEndpointFilter pluginGuardFilter)
    {
        // Excusal credits endpoints
        var credits = app.MapGroup("/api/v1")
            .AddEndpointFilter(pluginGuardFilter)
            .WithTags("ExcusalCredits");

        credits.MapGet("/excusal-credits", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            string? status,
            string? participant_email,
            int page = 1,
            int page_size = 20,
            CancellationToken ct = default) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            ExcusalCreditStatus? statusEnum = Enum.TryParse<ExcusalCreditStatus>(status, out var s) ? s : null;
            var result = await mediator.Send(
                new GetExcusalCreditsQuery(tenantId.Value, statusEnum, participant_email, page, Math.Clamp(page_size, 1, 100)), ct);
            return Results.Ok(result);
        }).RequireAuthorization("StaffOrAdmin");

        credits.MapPatch("/excusal-credits/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCreditRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var staffId = GetStaffId(ctx);
            await mediator.Send(new UpdateExcusalCreditCommand(id, tenantId.Value, staffId, req.AdditionalWindowIds, req.Tags), ct);
            return Results.Ok();
        }).RequireAuthorization("StaffOrAdmin");

        credits.MapDelete("/excusal-credits/{id:guid}", async (
            Guid id,
            ITenantContext tenantCtx,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var staffId = GetStaffId(ctx);
            await mediator.Send(new SoftDeleteExcusalCreditCommand(id, tenantId.Value, staffId), ct);
            return Results.NoContent();
        }).RequireAuthorization("StaffOrAdmin");

        // Excusal settings endpoints
        var settings = app.MapGroup("/api/v1")
            .AddEndpointFilter(pluginGuardFilter)
            .WithTags("ExcusalSettings");

        settings.MapGet("/settings/excusal-policy", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new GetTenantExcusalSettingsQuery(tenantId.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("StaffOrAdmin");

        settings.MapPatch("/settings/excusal-policy", async (
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
        }).RequireAuthorization("AdminOnly");

        settings.MapGet("/settings/excusal-windows", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new ListExcusalValidityWindowsQuery(tenantId.Value), ct);
            return Results.Ok(result);
        }).RequireAuthorization("StaffOrAdmin");

        settings.MapPost("/settings/excusal-windows", async (
            [FromBody] CreateWindowRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var id = await mediator.Send(new CreateExcusalValidityWindowCommand(tenantId.Value, req.Name, req.StartDate, req.EndDate), ct);
            return Results.Created($"/api/v1/settings/excusal-windows/{id}", new { window_id = id });
        }).RequireAuthorization("AdminOnly");

        settings.MapPatch("/settings/excusal-windows/{id:guid}", async (
            Guid id,
            [FromBody] UpdateWindowRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UpdateExcusalValidityWindowCommand(id, tenantId.Value, req.Name, req.StartDate, req.EndDate), ct);
            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        settings.MapDelete("/settings/excusal-windows/{id:guid}", async (
            Guid id,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new DeleteExcusalValidityWindowCommand(id, tenantId.Value), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");
    }

    private static Guid GetStaffId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public sealed record UpdateCreditRequest(List<Guid>? AdditionalWindowIds, List<string>? Tags);
public sealed record UpdateExcusalPolicyRequest(bool? CreditGenerationEnabled, int? ForwardWindowCount, int? UnenrollmentDeadlineDays, int? ExcusalDeadlineHours);
public sealed record CreateWindowRequest(string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record UpdateWindowRequest(string? Name, DateOnly? StartDate, DateOnly? EndDate);
