using MediatR;
using Microsoft.AspNetCore.Identity;
using Terminar.Modules.Identity.Domain;
using Terminar.Modules.Identity.Infrastructure.Identity;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Application.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandHandler(UserManager<AppIdentityUser> userManager)
    : IRequestHandler<CreateStaffUserCommand, CreateStaffUserResult>
{
    public async Task<CreateStaffUserResult> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
    {
        var existingByUsername = await userManager.FindByNameAsync(request.Username);
        if (existingByUsername is not null && existingByUsername.TenantId == request.TenantId.Value)
            throw new ConflictException($"Username '{request.Username}' is already taken in this tenant.");

        var existingByEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingByEmail is not null)
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var identityUser = new AppIdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = request.Username,
            Email = request.Email.ToLowerInvariant(),
            TenantId = request.TenantId.Value,
            TenantSlug = request.TenantSlug,
            Role = request.Role,
            IsActive = true
        };

        var result = await userManager.CreateAsync(identityUser, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ConflictException($"Failed to create staff user: {errors}");
        }

        return new CreateStaffUserResult(
            Guid.Parse(identityUser.Id),
            identityUser.UserName,
            identityUser.Email,
            identityUser.Role,
            DateTime.UtcNow);
    }
}
