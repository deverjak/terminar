using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Domain.Events;

public sealed record CustomFieldDefinitionDeleted(
    Guid EventId,
    DateTime OccurredAt,
    Guid FieldDefinitionId,
    Guid TenantId) : IDomainEvent;
