using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain.Events;

public sealed record TenantPluginDisabled(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    string PluginId) : IDomainEvent;
