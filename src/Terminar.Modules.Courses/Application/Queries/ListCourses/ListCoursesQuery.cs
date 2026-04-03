using MediatR;
using Terminar.Modules.Courses.Domain;

namespace Terminar.Modules.Courses.Application.Queries.ListCourses;

public sealed record CourseListItem(
    Guid Id,
    string Title,
    string Description,
    CourseType CourseType,
    RegistrationMode RegistrationMode,
    int Capacity,
    CourseStatus Status,
    int SessionCount,
    DateTime? FirstSessionAt,
    DateTime? LastSessionEndsAt,
    List<string> Tags);

public sealed record ListCoursesQuery(Guid TenantId) : IRequest<List<CourseListItem>>;
