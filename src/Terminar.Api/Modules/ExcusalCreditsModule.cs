using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Registrations.Application.Commands.SoftDeleteExcusalCredit;
using Terminar.Modules.Registrations.Application.Commands.UpdateExcusalCredit;
using Terminar.Modules.Registrations.Application.Queries.GetExcusalCredits;
using Terminar.Modules.Registrations.Domain;

namespace Terminar.Api.Modules;

public static class ExcusalCreditsModule
{
    public static IEndpointRouteBuilder MapExcusalCreditsEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/excusal-credits
        app.MapGet("/api/v1/excusal-credits", async (
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
        }).RequireAuthorization("StaffOrAdmin").WithTags("ExcusalCredits");

        // PATCH /api/v1/excusal-credits/{id}
        app.MapPatch("/api/v1/excusal-credits/{id:guid}", async (
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
        }).RequireAuthorization("StaffOrAdmin").WithTags("ExcusalCredits");

        // DELETE /api/v1/excusal-credits/{id}
        app.MapDelete("/api/v1/excusal-credits/{id:guid}", async (
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
        }).RequireAuthorization("StaffOrAdmin").WithTags("ExcusalCredits");

        return app;
    }

    private static Guid GetStaffId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public sealed record UpdateCreditRequest(List<Guid>? AdditionalWindowIds, List<string>? Tags);
