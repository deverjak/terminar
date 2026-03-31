using MediatR;
using Microsoft.AspNetCore.Identity;
using Terminar.Modules.Identity.Infrastructure.Identity;
using Terminar.SharedKernel;

namespace Terminar.Modules.Identity.Application.Commands.DeactivateStaffUser;

public sealed class DeactivateStaffUserCommandHandler(UserManager<AppIdentityUser> userManager)
    : IRequestHandler<DeactivateStaffUserCommand>
{
    public async Task Handle(DeactivateStaffUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.StaffUserId.ToString())
            ?? throw new NotFoundException($"Staff user '{request.StaffUserId}' not found.");

        user.IsActive = false;
        await userManager.UpdateAsync(user);
        await userManager.RemoveAuthenticationTokenAsync(user, "Default", "RefreshToken");
    }
}
