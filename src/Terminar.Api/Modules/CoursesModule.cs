using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Courses.Application.Commands.CancelCourse;
using Terminar.Modules.Courses.Application.Commands.CreateCourse;
using Terminar.Modules.Courses.Application.Commands.UpdateCourse;
using Terminar.Modules.Courses.Application.Commands.UpdateCourseExcusalPolicy;
using Terminar.Modules.Courses.Application.CustomFields;
using Terminar.Modules.Courses.Application.Queries.GetCourse;
using Terminar.Modules.Courses.Application.Queries.GetCourseExcusalPolicy;
using Terminar.Modules.Courses.Application.Queries.ListCourses;
using Terminar.Modules.Courses.Domain;

namespace Terminar.Api.Modules;

public static class CoursesModule
{
    public static IEndpointRouteBuilder MapCoursesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/courses")
            .RequireAuthorization("StaffOrAdmin")
            .WithTags("Courses");

        group.MapGet("/", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new ListCoursesQuery(tenantId.Value), ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new GetCourseQuery(id, tenantId.Value), ct);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            [FromBody] CreateCourseRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var staffId = GetStaffId(ctx);

            var command = new CreateCourseCommand(
                tenantId.Value,
                req.Title,
                req.Description ?? string.Empty,
                req.CourseType,
                req.RegistrationMode,
                req.Capacity,
                req.Sessions.Select(s => new SessionInput(s.ScheduledAt, s.DurationMinutes, s.Location)),
                staffId);

            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/courses/{id}", new { id });
        });

        group.MapPatch("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCourseRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UpdateCourseCommand(id, tenantId.Value, req.Title, req.Description, req.Capacity, req.RegistrationMode), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new CancelCourseCommand(id, tenantId.Value), ct);
            return Results.NoContent();
        });

        // GET /api/v1/courses/{courseId}/excusal-policy
        group.MapGet("/{courseId:guid}/excusal-policy", async (
            Guid courseId,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new GetCourseExcusalPolicyQuery(courseId, tenantId.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // PATCH /api/v1/courses/{courseId}/excusal-policy
        group.MapPatch("/{courseId:guid}/excusal-policy", async (
            Guid courseId,
            [FromBody] UpdateCourseExcusalPolicyRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UpdateCourseExcusalPolicyCommand(
                courseId, tenantId.Value,
                req.CreditGenerationOverride, req.ClearOverride ?? false,
                req.ValidityWindowId, req.ClearWindow ?? false,
                req.Tags), ct);
            return Results.Ok();
        });

        // GET /api/v1/courses/{courseId}/custom-fields
        group.MapGet("/{courseId:guid}/custom-fields", async (
            Guid courseId,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new GetCourseCustomFieldsQuery(courseId, tenantId.Value), ct);
            return Results.Ok(result);
        });

        // PUT /api/v1/courses/{courseId}/custom-fields
        group.MapPut("/{courseId:guid}/custom-fields", async (
            Guid courseId,
            [FromBody] UpdateCourseCustomFieldsRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            await mediator.Send(new UpdateCourseCustomFieldsCommand(courseId, tenantId.Value, req.EnabledFieldIds), ct);
            return Results.NoContent();
        });

        return app;
    }

    private static Guid GetStaffId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public sealed record SessionInputRequest(DateTime ScheduledAt, int DurationMinutes, string? Location);

public sealed record CreateCourseRequest(
    string Title,
    string? Description,
    CourseType CourseType,
    RegistrationMode RegistrationMode,
    int Capacity,
    List<SessionInputRequest> Sessions);

public sealed record UpdateCourseRequest(
    string? Title,
    string? Description,
    int? Capacity,
    RegistrationMode? RegistrationMode);

public sealed record UpdateCourseExcusalPolicyRequest(bool? CreditGenerationOverride, bool? ClearOverride, Guid? ValidityWindowId, bool? ClearWindow, List<string>? Tags);

public sealed record UpdateCourseCustomFieldsRequest(List<Guid> EnabledFieldIds);
