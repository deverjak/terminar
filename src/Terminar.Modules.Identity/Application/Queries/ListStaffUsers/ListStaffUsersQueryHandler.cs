using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Identity.Infrastructure.Identity;

namespace Terminar.Modules.Identity.Application.Queries.ListStaffUsers;

public sealed class ListStaffUsersQueryHandler(UserManager<AppIdentityUser> userManager)
    : IRequestHandler<ListStaffUsersQuery, IReadOnlyList<StaffUserDto>>
{
    public async Task<IReadOnlyList<StaffUserDto>> Handle(ListStaffUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .Where(u => u.TenantId == request.TenantId.Value)
            .ToListAsync(cancellationToken);

        return users.Select(u => new StaffUserDto(
            Guid.Parse(u.Id),
            u.UserName ?? string.Empty,
            u.Email ?? string.Empty,
            u.Role,
            u.IsActive ? "Active" : "Deactivated",
            DateTime.UtcNow)).ToList();
    }
}
