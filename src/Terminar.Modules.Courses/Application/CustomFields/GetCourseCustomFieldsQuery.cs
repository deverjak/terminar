using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Tenants.Application.CustomFields;

namespace Terminar.Modules.Courses.Application.CustomFields;

public sealed record GetCourseCustomFieldsQuery(Guid CourseId, Guid TenantId) : IRequest<List<CourseCustomFieldDto>>;

public sealed record CourseCustomFieldDto(
    Guid FieldDefinitionId,
    string Name,
    string FieldType,
    List<string> AllowedValues,
    int DisplayOrder,
    bool IsEnabled);

public sealed class GetCourseCustomFieldsHandler(CoursesDbContext db, IMediator mediator)
    : IRequestHandler<GetCourseCustomFieldsQuery, List<CourseCustomFieldDto>>
{
    public async Task<List<CourseCustomFieldDto>> Handle(
        GetCourseCustomFieldsQuery request,
        CancellationToken cancellationToken)
    {
        // Get all tenant field definitions via the Tenants module
        var allFields = await mediator.Send(
            new ListCustomFieldDefinitionsQuery(request.TenantId), cancellationToken);

        // Get enabled assignments for this course
        var enabledIds = await db.CourseFieldAssignments
            .Where(a => a.CourseId == request.CourseId)
            .Select(a => new { a.FieldDefinitionId, a.DisplayOrder })
            .ToListAsync(cancellationToken);

        var enabledMap = enabledIds.ToDictionary(a => a.FieldDefinitionId, a => a.DisplayOrder);

        return allFields
            .Select(f => new CourseCustomFieldDto(
                f.Id,
                f.Name,
                f.FieldType,
                f.AllowedValues,
                enabledMap.TryGetValue(f.Id, out var order) ? order : f.DisplayOrder,
                enabledMap.ContainsKey(f.Id)))
            .OrderBy(f => f.IsEnabled ? 0 : 1)
            .ThenBy(f => f.DisplayOrder)
            .ToList();
    }
}
