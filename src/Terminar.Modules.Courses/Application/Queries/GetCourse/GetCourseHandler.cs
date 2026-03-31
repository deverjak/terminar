using MediatR;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Courses.Application.Queries.GetCourse;

public sealed class GetCourseHandler(ICourseRepository repository) : IRequestHandler<GetCourseQuery, CourseDetail>
{
    public async Task<CourseDetail> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        var course = await repository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new NotFoundException($"Course '{request.CourseId}' not found.");

        if (course.TenantId.Value != request.TenantId)
            throw new NotFoundException($"Course '{request.CourseId}' not found.");

        var sessions = course.Sessions
            .OrderBy(s => s.Sequence)
            .Select(s => new SessionDto(s.Id, s.Sequence, s.ScheduledAt, s.DurationMinutes, s.Location, s.EndsAt))
            .ToList();

        return new CourseDetail(
            course.Id,
            course.Title,
            course.Description,
            course.CourseType,
            course.RegistrationMode,
            course.Capacity,
            course.Status,
            course.CreatedByStaffId,
            course.CreatedAt,
            course.UpdatedAt,
            sessions);
    }
}
