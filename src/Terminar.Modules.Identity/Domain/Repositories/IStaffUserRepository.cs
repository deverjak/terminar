using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Domain.Repositories;

public interface IStaffUserRepository
{
    Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StaffUser?> GetByUsernameAsync(TenantId tenantId, string username, CancellationToken ct = default);
    Task<IReadOnlyList<StaffUser>> FindByTenantAsync(TenantId tenantId, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(TenantId tenantId, string username, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(TenantId tenantId, string email, CancellationToken ct = default);
    Task AddAsync(StaffUser user, CancellationToken ct = default);
    Task UpdateAsync(StaffUser user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
