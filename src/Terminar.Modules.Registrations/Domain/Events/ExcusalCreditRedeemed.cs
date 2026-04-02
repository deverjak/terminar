using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain.Events;

public sealed record ExcusalCreditRedeemed(
    Guid EventId,
    DateTime OccurredAt,
    Guid CreditId,
    TenantId TenantId,
    Guid RedeemedCourseId,
    string ParticipantEmail) : IDomainEvent;
