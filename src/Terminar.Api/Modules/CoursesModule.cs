using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Api.Services;
using Terminar.Modules.Courses.Application.Commands.CancelCourse;
using Terminar.Modules.Courses.Application.Commands.CreateCourse;
using Terminar.Modules.Courses.Application.Commands.UpdateCourse;
using Terminar.Modules.Courses.Application.Commands.UpdateCourseExcusalPolicy;
using Terminar.Modules.Courses.Application.CustomFields;
using Terminar.Modules.Courses.Application.Queries.ExportCourses;
using Terminar.Modules.Courses.Application.Queries.GetCourse;
using Terminar.Modules.Courses.Application.Queries.GetCourseExcusalPolicy;
using Terminar.Modules.Courses.Application.Queries.ListCourses;
using Terminar.Modules.Courses.Domain;
using Terminar.Modules.Registrations.Application.Queries.ExportCourseRoster;
using Terminar.Modules.Registrations.Application.Queries.GetCourseEnrollmentCounts;
using Terminar.Modules.Tenants.Application.CustomFields;
using Terminar.Modules.Tenants.Domain.Repositories;

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

        // GET /api/v1/courses/export/columns
        group.MapGet("/export/columns", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            ITenantPluginActivationRepository pluginRepo,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId;

            var customFields = await mediator.Send(new ListCustomFieldDefinitionsQuery(tenantId.Value), ct);
            var excusalsActive = await pluginRepo.IsEnabledAsync(tenantId, "excusals", ct);

            var columns = new List<ExportColumnDefinition>
            {
                new("course_title", "export.columns.course_title", ExportColumnGroup.CourseInfo, true, false, "Course name"),
                new("course_status", "export.columns.course_status", ExportColumnGroup.CourseInfo, true, false, "Status"),
                new("course_first_session_at", "export.columns.course_first_session_at", ExportColumnGroup.CourseInfo, true, false, "Start date"),
                new("course_last_session_ends_at", "export.columns.course_last_session_ends_at", ExportColumnGroup.CourseInfo, true, false, "End date"),
                new("course_location", "export.columns.course_location", ExportColumnGroup.CourseInfo, true, false, "Location"),
                new("course_capacity", "export.columns.course_capacity", ExportColumnGroup.CourseInfo, true, false, "Capacity"),
                new("course_enrolled_count", "export.columns.course_enrolled_count", ExportColumnGroup.CourseInfo, true, false, "Enrolled"),
                new("course_waitlisted_count", "export.columns.course_waitlisted_count", ExportColumnGroup.CourseInfo, true, false, "Waitlisted"),
                new("course_type", "export.columns.course_type", ExportColumnGroup.CourseInfo, false, false, "Course type"),
                new("course_registration_mode", "export.columns.course_registration_mode", ExportColumnGroup.CourseInfo, false, false, "Registration mode"),
                new("course_description", "export.columns.course_description", ExportColumnGroup.CourseInfo, false, false, "Description"),
                new("participant_name", "export.columns.participant_name", ExportColumnGroup.ParticipantInfo, true, true, "Full name"),
                new("participant_email", "export.columns.participant_email", ExportColumnGroup.ParticipantInfo, true, true, "Email"),
                new("enrollment_status", "export.columns.enrollment_status", ExportColumnGroup.ParticipantInfo, true, true, "Enrollment status"),
                new("enrollment_date", "export.columns.enrollment_date", ExportColumnGroup.ParticipantInfo, true, true, "Enrolled on"),
            };

            if (excusalsActive)
                columns.Add(new("excusal_count", "export.columns.excusal_count", ExportColumnGroup.ParticipantInfo, false, true, "Excusal count"));

            foreach (var field in customFields)
                columns.Add(new($"cf_{field.Id}", "export.columns.custom_field", ExportColumnGroup.CustomFields, false, true, field.Name));

            return Results.Ok(new { columns });
        });

        // GET /api/v1/courses/export
        group.MapGet("/export", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            ITenantPluginActivationRepository pluginRepo,
            ICsvExportService csvService,
            HttpRequest request,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId;

            var columns = request.Query["columns"]
                .Where(c => c != null).Select(c => c!).ToList();
            if (columns.Count == 0)
                return Results.BadRequest(new { error = "At least one column must be selected." });

            var includeParticipants = request.Query["include_participants"] == "true";
            DateOnly? dateFrom = DateOnly.TryParse(request.Query["date_from"], out var df) ? df : null;
            DateOnly? dateTo = DateOnly.TryParse(request.Query["date_to"], out var dt) ? dt : null;
            CourseStatus? status = Enum.TryParse<CourseStatus>(request.Query["status"], out var cs) ? cs : null;

            if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
                return Results.BadRequest(new { error = "date_from must be before date_to." });

            // Fetch column definitions (needed for CSV header labels)
            var customFields = await mediator.Send(new ListCustomFieldDefinitionsQuery(tenantId.Value), ct);
            var excusalsActive = await pluginRepo.IsEnabledAsync(tenantId, "excusals", ct);

            var columnDefs = BuildColumnDefs(customFields, excusalsActive);

            var courses = await mediator.Send(new ExportCoursesQuery(tenantId.Value, dateFrom, dateTo, status), ct);
            var courseIds = courses.Select(c => c.CourseId).ToList();
            var counts = await mediator.Send(new GetCourseEnrollmentCountsQuery(tenantId.Value, courseIds), ct);

            byte[] csvBytes;

            if (includeParticipants)
            {
                var roster = await mediator.Send(
                    new ExportCourseRosterQuery(tenantId.Value, courseIds, excusalsActive), ct);
                var fieldDefs = roster.EnabledCustomFields;
                csvBytes = csvService.BuildWithParticipantsCsv(
                    courses, roster.Participants, fieldDefs, counts, columns, columnDefs);
            }
            else
            {
                csvBytes = csvService.BuildCoursesOnlyCsv(courses, counts, columns, columnDefs);
            }

            var filename = $"courses-export-{DateTime.Today:yyyy-MM-dd}.csv";
            return Results.File(csvBytes, "text/csv; charset=utf-8", filename);
        });

        // GET /api/v1/courses/{courseId}/registrations/export
        group.MapGet("/{courseId:guid}/registrations/export", async (
            Guid courseId,
            ITenantContext tenantCtx,
            IMediator mediator,
            ITenantPluginActivationRepository pluginRepo,
            ICsvExportService csvService,
            HttpRequest request,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId;

            var columns = request.Query["columns"]
                .Where(c => c != null).Select(c => c!).ToList();
            if (columns.Count == 0)
                return Results.BadRequest(new { error = "At least one column must be selected." });

            // Verify course belongs to tenant — GetCourseQuery throws NotFoundException if not found/wrong tenant,
            // which is handled by ExceptionHandlingMiddleware and returns 404.
            var course = await mediator.Send(new GetCourseQuery(courseId, tenantId.Value), ct);

            var excusalsActive = await pluginRepo.IsEnabledAsync(tenantId, "excusals", ct);
            var customFields = await mediator.Send(new ListCustomFieldDefinitionsQuery(tenantId.Value), ct);
            var columnDefs = BuildColumnDefs(customFields, excusalsActive);

            var courseDto = new ExportCourseRowDto(
                courseId,
                course.Title,
                course.Description,
                course.CourseType.ToString(),
                course.RegistrationMode.ToString(),
                course.Capacity,
                course.Status.ToString(),
                null, null, null);

            var roster = await mediator.Send(
                new ExportCourseRosterQuery(tenantId.Value, [courseId], excusalsActive), ct);

            var csvBytes = csvService.BuildWithParticipantsCsv(
                [courseDto], roster.Participants, roster.EnabledCustomFields,
                new Dictionary<Guid, (int, int)>(), columns, columnDefs);

            var filename = $"course-{courseId}-participants-{DateTime.Today:yyyy-MM-dd}.csv";
            return Results.File(csvBytes, "text/csv; charset=utf-8", filename);
        });

        return app;
    }

    private static List<ExportColumnDefinition> BuildColumnDefs(
        List<Terminar.Modules.Tenants.Application.CustomFields.CustomFieldDefinitionDto> customFields,
        bool excusalsActive)
    {
        var cols = new List<ExportColumnDefinition>
        {
            new("course_title", "export.columns.course_title", ExportColumnGroup.CourseInfo, true, false, "Course name"),
            new("course_status", "export.columns.course_status", ExportColumnGroup.CourseInfo, true, false, "Status"),
            new("course_first_session_at", "export.columns.course_first_session_at", ExportColumnGroup.CourseInfo, true, false, "Start date"),
            new("course_last_session_ends_at", "export.columns.course_last_session_ends_at", ExportColumnGroup.CourseInfo, true, false, "End date"),
            new("course_location", "export.columns.course_location", ExportColumnGroup.CourseInfo, true, false, "Location"),
            new("course_capacity", "export.columns.course_capacity", ExportColumnGroup.CourseInfo, true, false, "Capacity"),
            new("course_enrolled_count", "export.columns.course_enrolled_count", ExportColumnGroup.CourseInfo, true, false, "Enrolled"),
            new("course_waitlisted_count", "export.columns.course_waitlisted_count", ExportColumnGroup.CourseInfo, true, false, "Waitlisted"),
            new("course_type", "export.columns.course_type", ExportColumnGroup.CourseInfo, false, false, "Course type"),
            new("course_registration_mode", "export.columns.course_registration_mode", ExportColumnGroup.CourseInfo, false, false, "Registration mode"),
            new("course_description", "export.columns.course_description", ExportColumnGroup.CourseInfo, false, false, "Description"),
            new("participant_name", "export.columns.participant_name", ExportColumnGroup.ParticipantInfo, true, true, "Full name"),
            new("participant_email", "export.columns.participant_email", ExportColumnGroup.ParticipantInfo, true, true, "Email"),
            new("enrollment_status", "export.columns.enrollment_status", ExportColumnGroup.ParticipantInfo, true, true, "Enrollment status"),
            new("enrollment_date", "export.columns.enrollment_date", ExportColumnGroup.ParticipantInfo, true, true, "Enrolled on"),
        };

        if (excusalsActive)
            cols.Add(new("excusal_count", "export.columns.excusal_count", ExportColumnGroup.ParticipantInfo, false, true, "Excusal count"));

        foreach (var field in customFields)
            cols.Add(new($"cf_{field.Id}", "export.columns.custom_field", ExportColumnGroup.CustomFields, false, true, field.Name));

        return cols;
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
