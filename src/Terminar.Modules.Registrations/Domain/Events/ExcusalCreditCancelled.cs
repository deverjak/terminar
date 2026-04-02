using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record ExcusalCreditCancelled(
    Guid EventId,
    DateTime OccurredAt,
    Guid CreditId,
    TenantId TenantId) : IDomainEvent;
