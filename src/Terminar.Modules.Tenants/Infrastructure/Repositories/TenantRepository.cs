using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Infrastructure.Repositories;

public sealed class TenantRepository(TenantsDbContext dbContext) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default) =>
        await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await dbContext.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default) =>
        await dbContext.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default) =>
        await dbContext.Tenants.AddAsync(tenant, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        dbContext.SaveChangesAsync(ct);
}
