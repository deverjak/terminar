using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.CancelRegistration;

public sealed record CancelRegistrationCommand(
    Guid RegistrationId,
    Guid CourseId,
    Guid TenantId,
    Guid? SelfCancellationToken,
    Guid? StaffUserId) : IRequest;
