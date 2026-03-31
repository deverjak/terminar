using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.CreateRegistration;

public sealed record CreateRegistrationCommand(
    Guid TenantId,
    Guid CourseId,
    string ParticipantName,
    string ParticipantEmail,
    Guid? RegisteredByStaffId) : IRequest<CreateRegistrationResult>;

public sealed record CreateRegistrationResult(
    Guid RegistrationId,
    Guid CourseId,
    string ParticipantName,
    string ParticipantEmail,
    string RegistrationSource,
    string Status,
    DateTime RegisteredAt,
    Guid SelfCancellationToken);
