using MediatR;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Registrations.Domain.Services;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.Commands.CreateRegistration;

public sealed class CreateRegistrationCommandHandler(
    IRegistrationRepository repository,
    RegistrationCapacityChecker capacityChecker) : IRequestHandler<CreateRegistrationCommand, CreateRegistrationResult>
{
    public async Task<CreateRegistrationResult> Handle(CreateRegistrationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var email = Email.From(request.ParticipantEmail);
        var isStaff = request.RegisteredByStaffId.HasValue;

        // Phase 4+5: capacity + course status check (capacity checker also validates course is Active)
        var courseInfo = await capacityChecker.EnsureCanRegisterAsync(request.CourseId, request.TenantId, cancellationToken);

        // Enforce registration mode for self-service callers
        if (!isStaff && courseInfo.RegistrationMode == "StaffOnly")
            throw new ForbiddenException("This course requires staff to add participants.");

        // Duplicate registration check
        var existing = await repository.GetByEmailAndCourseAsync(
            email.Value, request.CourseId, request.TenantId, cancellationToken);

        if (existing is not null && existing.Status == RegistrationStatus.Confirmed)
            throw new ConflictException($"Participant '{email.Value}' is already registered for this course.");

        var source = isStaff ? RegistrationSource.StaffAdded : RegistrationSource.SelfService;

        var registration = Registration.Create(
            tenantId,
            request.CourseId,
            request.ParticipantName,
            email,
            source,
            request.RegisteredByStaffId);

        await repository.AddAsync(registration, cancellationToken);

        return new CreateRegistrationResult(
            registration.Id,
            registration.CourseId,
            registration.ParticipantName,
            registration.ParticipantEmail.Value,
            registration.RegistrationSource.ToString(),
            registration.Status.ToString(),
            registration.RegisteredAt,
            registration.SelfCancellationToken);
    }
}
