using MediatR;
using Terminar.Modules.Courses.Domain.Repositories;

namespace Terminar.Modules.Courses.Application.Queries.ListCourses;

public sealed class ListCoursesHandler(ICourseRepository repository) : IRequestHandler<ListCoursesQuery, List<CourseListItem>>
{
    public async Task<List<CourseListItem>> Handle(ListCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await repository.ListByTenantAsync(request.TenantId, cancellationToken);

        return courses.Select(c => new CourseListItem(
            c.Id,
            c.Title,
            c.Description,
            c.CourseType,
            c.RegistrationMode,
            c.Capacity,
            c.Status,
            c.Sessions.Count,
            c.Sessions.MinBy(s => s.ScheduledAt)?.ScheduledAt,
            c.Sessions.MaxBy(s => s.ScheduledAt)?.EndsAt,
            c.ExcusalPolicy.Tags)).ToList();
    }
}
