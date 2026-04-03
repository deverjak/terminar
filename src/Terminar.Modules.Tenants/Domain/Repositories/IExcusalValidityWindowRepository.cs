namespace Terminar.Modules.Tenants.Domain.Repositories;

public interface IExcusalValidityWindowRepository
{
    Task<ExcusalValidityWindow?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<ExcusalValidityWindow>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(ExcusalValidityWindow window, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
