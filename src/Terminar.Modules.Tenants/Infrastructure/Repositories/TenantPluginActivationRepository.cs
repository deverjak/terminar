using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Infrastructure.Repositories;

public sealed class TenantPluginActivationRepository(TenantsDbContext db) : ITenantPluginActivationRepository
{
    public async Task<bool> IsEnabledAsync(TenantId tenantId, string pluginId, CancellationToken ct = default)
        => await db.TenantPluginActivations
            .AnyAsync(x => x.TenantId == tenantId && x.PluginId == pluginId && x.IsEnabled, ct);

    public async Task<IReadOnlyList<TenantPluginActivation>> ListForTenantAsync(TenantId tenantId, CancellationToken ct = default)
        => await db.TenantPluginActivations
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<TenantPluginActivation?> FindAsync(TenantId tenantId, string pluginId, CancellationToken ct = default)
        => await db.TenantPluginActivations
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.PluginId == pluginId, ct);

    public async Task AddAsync(TenantPluginActivation activation, CancellationToken ct = default)
        => await db.TenantPluginActivations.AddAsync(activation, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
