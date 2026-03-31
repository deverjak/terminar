using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record RegistrationCreated(
    Guid EventId,
    DateTime OccurredAt,
    Guid RegistrationId,
    Guid CourseId,
    TenantId TenantId,
    string ParticipantEmail) : IDomainEvent;
