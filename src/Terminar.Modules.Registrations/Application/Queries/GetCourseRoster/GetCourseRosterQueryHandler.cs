using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.CustomFields;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Infrastructure;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;

public sealed class GetCourseRosterQueryHandler(RegistrationsDbContext db, IMediator mediator)
    : IRequestHandler<GetCourseRosterQuery, GetCourseRosterResult>
{
    public async Task<GetCourseRosterResult> Handle(GetCourseRosterQuery request, CancellationToken cancellationToken)
    {
        var tid = TenantId.From(request.TenantId);

        var query = db.Registrations
            .Where(r => r.CourseId == request.CourseId && r.TenantId == tid);

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<RegistrationStatus>(request.StatusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);

        var registrations = await query
            .OrderBy(r => r.RegisteredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(r => r.FieldValues)
            .ToListAsync(cancellationToken);

        // Get enabled custom fields for this course via Courses module
        var courseFields = await mediator.Send(
            new GetCourseCustomFieldsQuery(request.CourseId, request.TenantId), cancellationToken);

        var enabledFields = courseFields
            .Where(f => f.IsEnabled)
            .Select(f => new EnabledCustomFieldDto(
                f.FieldDefinitionId, f.Name, f.FieldType, f.AllowedValues, f.DisplayOrder))
            .ToList();

        var enabledFieldIds = enabledFields.Select(f => f.FieldDefinitionId).ToHashSet();

        // Build per-registration field value maps
        var items = registrations.Select(r => new RegistrationDto(
            r.Id,
            r.ParticipantName,
            r.ParticipantEmail.Value,
            r.RegistrationSource.ToString(),
            r.Status.ToString(),
            r.RegisteredAt,
            r.FieldValues
                .Where(v => enabledFieldIds.Contains(v.FieldDefinitionId))
                .ToDictionary(v => v.FieldDefinitionId, v => v.Value)))
            .ToList();

        // Compute summary counts (non-null values per field) across ALL registrations in the course
        var allRegistrationIds = await db.Registrations
            .Where(r => r.CourseId == request.CourseId && r.TenantId == tid)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var fieldValueSummary = await db.ParticipantFieldValues
            .Where(v => allRegistrationIds.Contains(v.RegistrationId)
                        && enabledFieldIds.Contains(v.FieldDefinitionId)
                        && v.Value != null)
            .GroupBy(v => v.FieldDefinitionId)
            .Select(g => new { FieldId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var summary = fieldValueSummary.ToDictionary(x => x.FieldId, x => x.Count);

        return new GetCourseRosterResult(items, total, request.Page, request.PageSize, enabledFields, summary);
    }
}
