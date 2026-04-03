using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record ExcusalCreditIssued(
    Guid EventId,
    DateTime OccurredAt,
    Guid CreditId,
    TenantId TenantId,
    string ParticipantEmail,
    Guid SourceExcusalId) : IDomainEvent;
