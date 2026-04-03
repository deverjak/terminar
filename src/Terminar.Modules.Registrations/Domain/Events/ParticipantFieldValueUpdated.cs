using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record ParticipantFieldValueUpdated(
    Guid EventId,
    DateTime OccurredAt,
    Guid RegistrationId,
    Guid FieldDefinitionId,
    string? Value,
    TenantId TenantId) : IDomainEvent;
