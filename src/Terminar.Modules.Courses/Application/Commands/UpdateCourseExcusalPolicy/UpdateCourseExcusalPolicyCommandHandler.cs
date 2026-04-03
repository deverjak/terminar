using MediatR;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Application.Commands.UpdateCourseExcusalPolicy;

public sealed class UpdateCourseExcusalPolicyCommandHandler(ICourseRepository courseRepo)
    : IRequestHandler<UpdateCourseExcusalPolicyCommand>
{
    public async Task Handle(UpdateCourseExcusalPolicyCommand request, CancellationToken cancellationToken)
    {
        var course = await courseRepo.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new NotFoundException("Course not found.");

        if (course.TenantId != TenantId.From(request.TenantId))
            throw new NotFoundException("Course not found.");

        var overrideValue = request.ClearOverride ? null : request.CreditGenerationOverride;
        var windowId = request.ClearWindow ? null : request.ValidityWindowId;
        course.UpdateExcusalPolicy(overrideValue, windowId, request.Tags);
        await courseRepo.UpdateAsync(course, cancellationToken);
    }
}
