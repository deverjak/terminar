using MediatR;
using Terminar.Modules.Courses.Domain;

namespace Terminar.Modules.Courses.Application.Queries.GetCourse;

public sealed record SessionDto(
    Guid Id,
    int Sequence,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? Location,
    DateTimeOffset EndsAt);

public sealed record CourseDetail(
    Guid Id,
    string Title,
    string Description,
    CourseType CourseType,
    RegistrationMode RegistrationMode,
    int Capacity,
    CourseStatus Status,
    Guid CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<SessionDto> Sessions);

public sealed record GetCourseQuery(Guid CourseId, Guid TenantId) : IRequest<CourseDetail>;
