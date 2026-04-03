using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Api.Notifications;
using Terminar.Modules.Registrations.Application.Commands.CreateExcusal;
using Terminar.Modules.Registrations.Application.Commands.RequestMagicLink;
using Terminar.Modules.Registrations.Application.Commands.RedeemExcusalCredit;
using Terminar.Modules.Registrations.Application.Commands.RedeemMagicLink;
using Terminar.Modules.Registrations.Application.Commands.UnenrollViaSafeLink;
using Terminar.Modules.Registrations.Application.Queries.GetParticipantCourseView;
using Terminar.Modules.Registrations.Application.Queries.GetParticipantPortal;

namespace Terminar.Api.Modules;

public static class ParticipantsModule
{
    public static IEndpointRouteBuilder MapParticipantsEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/v1/participants/magic-link — request a magic link (public)
        // TODO: Add rate limiting (e.g., 5 requests per email per hour) before production to prevent abuse.
        app.MapPost("/api/v1/participants/magic-link", async (
            [FromBody] RequestMagicLinkRequest req,
            ITenantContext tenantCtx,
            IEmailNotificationService emailService,
            IMediator mediator,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new RequestMagicLinkCommand(tenantId.Value, req.Email), ct);

            if (result.WasSent)
            {
                var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5173";
                var magicLinkUrl = $"{baseUrl}/participant/portal?token={result.MagicLinkToken}";
                await emailService.SendMagicLinkAsync(req.Email, req.Email, magicLinkUrl, ct);
            }

            // Always return 202 to prevent email enumeration
            return Results.Accepted();
        }).AllowAnonymous().WithTags("Participants");

        // POST /api/v1/participants/portal/redeem — exchange magic link for portal token
        app.MapPost("/api/v1/participants/portal/redeem", async (
            [FromBody] RedeemMagicLinkRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new RedeemMagicLinkCommand(req.Token), ct);
            return Results.Ok(new { portal_token = result.PortalToken, expires_at = result.ExpiresAt });
        }).AllowAnonymous().WithTags("Participants");

        // GET /api/v1/participants/courses/{safeLinkToken} — course view via safe link
        app.MapGet("/api/v1/participants/courses/{safeLinkToken:guid}", async (
            Guid safeLinkToken,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new GetParticipantCourseViewQuery(safeLinkToken, tenantId.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous().WithTags("Participants");

        // POST /api/v1/participants/courses/{safeLinkToken}/unenroll — self-unenroll
        app.MapPost("/api/v1/participants/courses/{safeLinkToken:guid}/unenroll", async (
            Guid safeLinkToken,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UnenrollViaSafeLinkCommand(safeLinkToken, tenantId.Value), ct);
            return Results.Ok(new { message = "Successfully unenrolled." });
        }).AllowAnonymous().WithTags("Participants");

        // POST /api/v1/participants/courses/{safeLinkToken}/sessions/{sessionId}/excuse — excuse from session
        app.MapPost("/api/v1/participants/courses/{safeLinkToken:guid}/sessions/{sessionId:guid}/excuse", async (
            Guid safeLinkToken,
            Guid sessionId,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new CreateExcusalCommand(safeLinkToken, sessionId, tenantId.Value), ct);
            return Results.Ok(new { excusal_id = result.ExcusalId, credit_issued = result.CreditIssued });
        }).AllowAnonymous().WithTags("Participants");

        // GET /api/v1/participants/portal — participant dashboard
        app.MapGet("/api/v1/participants/portal", async (
            HttpContext ctx,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var portalToken = ctx.Request.Headers["X-Portal-Token"].FirstOrDefault()
                ?? throw new UnauthorizedAccessException("Portal token missing.");

            var result = await mediator.Send(new GetParticipantPortalQuery(portalToken, tenantId.Value), ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).AllowAnonymous().WithTags("Participants");

        // POST /api/v1/participants/credits/{creditId}/redeem — redeem excusal credit
        app.MapPost("/api/v1/participants/credits/{creditId:guid}/redeem", async (
            Guid creditId,
            [FromBody] RedeemCreditRequest req,
            HttpContext ctx,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var portalToken = ctx.Request.Headers["X-Portal-Token"].FirstOrDefault()
                ?? throw new UnauthorizedAccessException("Portal token missing.");

            var result = await mediator.Send(new RedeemExcusalCreditCommand(creditId, req.TargetCourseId, portalToken, tenantId.Value), ct);
            return Results.Ok(new { new_enrollment_id = result.NewEnrollmentId, safe_link_token = result.SafeLinkToken });
        }).AllowAnonymous().WithTags("Participants");

        return app;
    }
}

public sealed record RequestMagicLinkRequest(string Email);
public sealed record RedeemMagicLinkRequest(string Token);
public sealed record RedeemCreditRequest(Guid TargetCourseId);
