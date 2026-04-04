using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain.Repositories;

public interface ITenantPluginActivationRepository
{
    Task<bool> IsEnabledAsync(TenantId tenantId, string pluginId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantPluginActivation>> ListForTenantAsync(TenantId tenantId, CancellationToken ct = default);
    Task<TenantPluginActivation?> FindAsync(TenantId tenantId, string pluginId, CancellationToken ct = default);
    Task AddAsync(TenantPluginActivation activation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
