using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Identity.Domain;
using Terminar.Modules.Identity.Domain.Repositories;
using Terminar.Modules.Identity.Infrastructure.Identity;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Infrastructure.Persistence;

public sealed class StaffUserRepository(UserManager<AppIdentityUser> userManager) : IStaffUserRepository
{
    public async Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : MapToDomain(user);
    }

    public async Task<StaffUser?> GetByUsernameAsync(TenantId tenantId, string username, CancellationToken ct = default)
    {
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId.Value && u.UserName == username, ct);
        return user is null ? null : MapToDomain(user);
    }

    public async Task<IReadOnlyList<StaffUser>> FindByTenantAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var users = await userManager.Users
            .Where(u => u.TenantId == tenantId.Value)
            .ToListAsync(ct);
        return users.Select(MapToDomain).ToList();
    }

    public async Task<bool> ExistsByUsernameAsync(TenantId tenantId, string username, CancellationToken ct = default) =>
        await userManager.Users.AnyAsync(u => u.TenantId == tenantId.Value && u.UserName == username, ct);

    public async Task<bool> ExistsByEmailAsync(TenantId tenantId, string email, CancellationToken ct = default) =>
        await userManager.Users.AnyAsync(u => u.TenantId == tenantId.Value && u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(StaffUser user, CancellationToken ct = default)
    {
        // StaffUser without password — password is set via CreateWithPasswordAsync
        throw new NotSupportedException("Use CreateWithPasswordAsync to create staff users.");
    }

    public async Task<IdentityResult> CreateWithPasswordAsync(StaffUser user, string password)
    {
        var identityUser = new AppIdentityUser
        {
            Id = user.Id.ToString(),
            UserName = user.Username,
            Email = user.Email.Value,
            TenantId = user.TenantId.Value,
            Role = user.Role.ToString(),
            IsActive = user.IsActive()
        };
        return await userManager.CreateAsync(identityUser, password);
    }

    public async Task UpdateAsync(StaffUser user, CancellationToken ct = default)
    {
        var identityUser = await userManager.FindByIdAsync(user.Id.ToString());
        if (identityUser is null) return;
        identityUser.IsActive = user.IsActive();
        identityUser.Role = user.Role.ToString();
        await userManager.UpdateAsync(identityUser);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask; // UserManager handles persistence

    private static StaffUser MapToDomain(AppIdentityUser user)
    {
        var staffUser = StaffUser.Create(
            TenantId.From(user.TenantId),
            user.UserName ?? string.Empty,
            Email.From(user.Email ?? string.Empty),
            Enum.Parse<StaffRole>(user.Role));

        if (!user.IsActive)
            staffUser.Deactivate();

        return staffUser;
    }
}
