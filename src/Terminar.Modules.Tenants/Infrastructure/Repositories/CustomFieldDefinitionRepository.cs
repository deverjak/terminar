using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Infrastructure.Repositories;

public sealed class CustomFieldDefinitionRepository(TenantsDbContext db) : ICustomFieldDefinitionRepository
{
    public async Task<List<CustomFieldDefinition>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.CustomFieldDefinitions
            .Where(x => x.TenantId == tid)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<CustomFieldDefinition?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.CustomFieldDefinitions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid, ct);
    }

    public async Task<bool> ExistsByNameAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.CustomFieldDefinitions
            .AnyAsync(x => x.TenantId == tid
                && x.Name == name
                && (excludeId == null || x.Id != excludeId.Value), ct);
    }

    public async Task AddAsync(CustomFieldDefinition field, CancellationToken ct = default) =>
        await db.CustomFieldDefinitions.AddAsync(field, ct);

    public Task DeleteAsync(CustomFieldDefinition field, CancellationToken ct = default)
    {
        db.CustomFieldDefinitions.Remove(field);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
