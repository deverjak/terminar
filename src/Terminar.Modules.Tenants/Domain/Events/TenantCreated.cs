using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain.Events;

public sealed record TenantCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    string Name,
    string Slug) : IDomainEvent;
