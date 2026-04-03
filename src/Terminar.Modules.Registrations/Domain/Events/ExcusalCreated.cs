using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record ExcusalCreated(
    Guid EventId,
    DateTime OccurredAt,
    Guid ExcusalId,
    Guid RegistrationId,
    Guid CourseId,
    Guid SessionId,
    TenantId TenantId,
    string ParticipantEmail,
    string ParticipantName) : IDomainEvent;
