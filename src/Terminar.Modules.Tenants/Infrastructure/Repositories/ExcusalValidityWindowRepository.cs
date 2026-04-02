using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Infrastructure.Repositories;

public sealed class ExcusalValidityWindowRepository(TenantsDbContext db) : IExcusalValidityWindowRepository
{
    public async Task<ExcusalValidityWindow?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.ExcusalValidityWindows
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid, ct);
    }

    public async Task<List<ExcusalValidityWindow>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.ExcusalValidityWindows
            .Where(x => x.TenantId == tid)
            .OrderBy(x => x.StartDate)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.ExcusalValidityWindows
            .AnyAsync(x => x.TenantId == tid && x.Name == name && (excludeId == null || x.Id != excludeId), ct);
    }

    public async Task AddAsync(ExcusalValidityWindow window, CancellationToken ct = default)
        => await db.ExcusalValidityWindows.AddAsync(window, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
