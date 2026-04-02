using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record RegistrationCancelled(
    Guid EventId,
    DateTime OccurredAt,
    Guid RegistrationId,
    Guid CourseId,
    TenantId TenantId,
    string ParticipantEmail,
    string ParticipantName) : IDomainEvent;
