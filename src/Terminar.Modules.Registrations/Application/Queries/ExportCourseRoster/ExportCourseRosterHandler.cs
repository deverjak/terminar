using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.CustomFields;
using Terminar.Modules.Registrations.Infrastructure;
using Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.Queries.ExportCourseRoster;

public sealed class ExportCourseRosterHandler(RegistrationsDbContext db, IMediator mediator)
    : IRequestHandler<ExportCourseRosterQuery, ExportCourseRosterResult>
{
    public async Task<ExportCourseRosterResult> Handle(
        ExportCourseRosterQuery request,
        CancellationToken cancellationToken)
    {
        var tid = TenantId.From(request.TenantId);
        var courseIds = request.CourseIds.ToList();

        var registrations = await db.Registrations
            .Include(r => r.FieldValues)
            .Where(r => r.TenantId == tid && courseIds.Contains(r.CourseId))
            .OrderBy(r => r.RegisteredAt)
            .ToListAsync(cancellationToken);

        // Get enabled custom fields across all courses (per-course may differ; use first course for simplicity
        // or fetch tenant-level fields). We use the first courseId for cross-module query.
        var registrationIds = registrations.Select(r => r.Id).ToHashSet();

        // Fetch custom field definitions per course and collect union of all enabled fields
        var enabledFieldsMap = new Dictionary<Guid, EnabledCustomFieldDto>();
        foreach (var courseId in courseIds)
        {
            var courseFields = await mediator.Send(
                new GetCourseCustomFieldsQuery(courseId, request.TenantId), cancellationToken);
            foreach (var f in courseFields.Where(f => f.IsEnabled))
            {
                enabledFieldsMap.TryAdd(f.FieldDefinitionId,
                    new EnabledCustomFieldDto(f.FieldDefinitionId, f.Name, f.FieldType, f.AllowedValues, f.DisplayOrder));
            }
        }

        var enabledFieldIds = enabledFieldsMap.Keys.ToHashSet();

        // Optionally fetch excusal counts per registration
        Dictionary<Guid, int>? excusalCounts = null;
        if (request.IncludeExcusalCounts && registrations.Count > 0)
        {
            var counts = await db.Excusals
                .Where(e => e.TenantId == tid && registrationIds.Contains(e.RegistrationId))
                .GroupBy(e => e.RegistrationId)
                .Select(g => new { RegistrationId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            excusalCounts = counts.ToDictionary(x => x.RegistrationId, x => x.Count);
        }

        var participants = registrations.Select(r => new ExportParticipantRowDto(
            r.CourseId,
            r.ParticipantName,
            r.ParticipantEmail.Value,
            r.Status.ToString(),
            DateOnly.FromDateTime(r.RegisteredAt),
            r.FieldValues
                .Where(v => enabledFieldIds.Contains(v.FieldDefinitionId))
                .ToDictionary(v => v.FieldDefinitionId, v => v.Value),
            excusalCounts != null
                ? (excusalCounts.TryGetValue(r.Id, out var cnt) ? cnt : 0)
                : null
        )).ToList();

        var enabledFields = enabledFieldsMap.Values
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        return new ExportCourseRosterResult(participants, enabledFields);
    }
}
