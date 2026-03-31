using MediatR;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Courses.Application.Commands.UpdateCourse;

public sealed class UpdateCourseHandler(ICourseRepository repository) : IRequestHandler<UpdateCourseCommand>
{
    public async Task Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await repository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new NotFoundException($"Course '{request.CourseId}' not found.");

        if (course.TenantId.Value != request.TenantId)
            throw new ForbiddenException("Course does not belong to the current tenant.");

        course.Update(request.Title, request.Description, request.Capacity, request.RegistrationMode);

        await repository.UpdateAsync(course, cancellationToken);
    }
}
