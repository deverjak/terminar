using MediatR;
using Terminar.Modules.Courses.Application.Ports;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Registrations.Application.Commands.CancelRegistration;

public sealed class CancelRegistrationCommandHandler(
    IRegistrationRepository repository,
    ICourseCapacityReader courseCapacityReader) : IRequestHandler<CancelRegistrationCommand>
{
    public async Task Handle(CancelRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetByIdAsync(
            request.RegistrationId, request.TenantId, cancellationToken);

        if (registration is null)
            throw new NotFoundException($"Registration '{request.RegistrationId}' not found.");

        if (registration.CourseId != request.CourseId)
            throw new NotFoundException($"Registration '{request.RegistrationId}' not found for this course.");

        var isStaff = request.StaffUserId.HasValue;

        if (!isStaff)
        {
            if (!request.SelfCancellationToken.HasValue ||
                request.SelfCancellationToken.Value != registration.SelfCancellationToken)
                throw new ForbiddenException("Invalid or missing cancellation token.");
        }

        var courseInfo = await courseCapacityReader.GetCapacityInfoAsync(
            request.CourseId, request.TenantId, cancellationToken);

        var now = DateTime.UtcNow;
        registration.Cancel(now, courseInfo?.LastSessionEndsAt);

        await repository.UpdateAsync(registration, cancellationToken);
    }
}
