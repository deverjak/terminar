using MediatR;
using Terminar.Modules.Courses.Domain;

namespace Terminar.Modules.Courses.Application.Commands.CreateCourse;

public sealed record SessionInput(DateTimeOffset ScheduledAt, int DurationMinutes, string? Location);

public sealed record CreateCourseCommand(
    Guid TenantId,
    string Title,
    string Description,
    CourseType CourseType,
    RegistrationMode RegistrationMode,
    int Capacity,
    IEnumerable<SessionInput> Sessions,
    Guid CreatedByStaffId) : IRequest<Guid>;
