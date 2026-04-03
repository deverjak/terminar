using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Registrations.Application.Commands.CancelRegistration;
using Terminar.Modules.Registrations.Application.Commands.CreateRegistration;
using Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;

namespace Terminar.Api.Modules;

public static class RegistrationsModule
{
    public static IEndpointRouteBuilder MapRegistrationsEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/v1/courses/{courseId}/registrations — public (Open courses) or Staff
        app.MapPost("/api/v1/courses/{courseId:guid}/registrations", async (
            Guid courseId,
            [FromBody] CreateRegistrationRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var staffId = GetOptionalStaffId(ctx);

            var command = new CreateRegistrationCommand(
                tenantId.Value,
                courseId,
                req.ParticipantName,
                req.ParticipantEmail,
                staffId);

            var result = await mediator.Send(command, ct);

            return Results.Created(
                $"/api/v1/courses/{courseId}/registrations/{result.RegistrationId}",
                new
                {
                    registration_id = result.RegistrationId,
                    course_id = result.CourseId,
                    participant_name = result.ParticipantName,
                    participant_email = result.ParticipantEmail,
                    registration_source = result.RegistrationSource,
                    status = result.Status,
                    registered_at = result.RegisteredAt,
                    safe_link_token = result.SafeLinkToken
                });
        }).AllowAnonymous().WithTags("Registrations");

        // GET /api/v1/courses/{courseId}/registrations — Staff/Admin only
        app.MapGet("/api/v1/courses/{courseId:guid}/registrations", async (
            Guid courseId,
            ITenantContext tenantCtx,
            IMediator mediator,
            string? status,
            int page = 1,
            int page_size = 20,
            CancellationToken ct = default) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(
                new GetCourseRosterQuery(courseId, tenantId.Value, status, page, Math.Clamp(page_size, 1, 100)), ct);

            return Results.Ok(new
            {
                items = result.Items,
                total = result.Total,
                page = result.Page,
                page_size = result.PageSize
            });
        }).RequireAuthorization("StaffOrAdmin").WithTags("Registrations");

        // DELETE /api/v1/courses/{courseId}/registrations/{registrationId} — public with token, or Staff
        app.MapDelete("/api/v1/courses/{courseId:guid}/registrations/{registrationId:guid}", async (
            Guid courseId,
            Guid registrationId,
            ITenantContext tenantCtx,
            IMediator mediator,
            HttpContext ctx,
            string? token,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var staffId = GetOptionalStaffId(ctx);

            Guid? cancellationToken = Guid.TryParse(token, out var t) ? t : null;

            var command = new CancelRegistrationCommand(
                registrationId,
                courseId,
                tenantId.Value,
                cancellationToken,
                staffId);

            await mediator.Send(command, ct);
            return Results.NoContent();
        }).AllowAnonymous().WithTags("Registrations");

        return app;
    }

    private static Guid? GetOptionalStaffId(HttpContext ctx)
    {
        if (!ctx.User.Identity?.IsAuthenticated ?? true) return null;
        var sub = ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record CreateRegistrationRequest(string ParticipantName, string ParticipantEmail);
