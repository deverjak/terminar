using MediatR;
using Terminar.Modules.Courses.Domain;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Application.Commands.CreateCourse;

public sealed class CreateCourseHandler(ICourseRepository repository) : IRequestHandler<CreateCourseCommand, Guid>
{
    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var sessionInputs = request.Sessions
            .Select(s => (s.ScheduledAt, s.DurationMinutes, s.Location));

        var course = Course.Create(
            tenantId,
            request.Title,
            request.Description,
            request.CourseType,
            request.RegistrationMode,
            request.Capacity,
            sessionInputs,
            request.CreatedByStaffId);

        await repository.AddAsync(course, cancellationToken);

        return course.Id;
    }
}
