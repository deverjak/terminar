using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Domain.Events;

public sealed record StaffUserCreated(
    Guid EventId,
    DateTime OccurredAt,
    Guid StaffUserId,
    TenantId TenantId,
    string Username) : IDomainEvent;
