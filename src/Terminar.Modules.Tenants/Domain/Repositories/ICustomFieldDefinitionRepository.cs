namespace Terminar.Modules.Tenants.Domain.Repositories;

public interface ICustomFieldDefinitionRepository
{
    Task<List<CustomFieldDefinition>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<CustomFieldDefinition?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(CustomFieldDefinition field, CancellationToken ct = default);
    Task DeleteAsync(CustomFieldDefinition field, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
