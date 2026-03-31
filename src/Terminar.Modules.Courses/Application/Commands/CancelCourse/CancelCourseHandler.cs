using MediatR;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Courses.Application.Commands.CancelCourse;

public sealed class CancelCourseHandler(ICourseRepository repository) : IRequestHandler<CancelCourseCommand>
{
    public async Task Handle(CancelCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await repository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new NotFoundException($"Course '{request.CourseId}' not found.");

        if (course.TenantId.Value != request.TenantId)
            throw new ForbiddenException("Course does not belong to the current tenant.");

        course.Cancel();

        await repository.UpdateAsync(course, cancellationToken);
    }
}
