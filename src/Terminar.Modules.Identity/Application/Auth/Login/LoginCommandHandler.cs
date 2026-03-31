using MediatR;
using Microsoft.AspNetCore.Identity;
using Terminar.Modules.Identity.Infrastructure.Identity;
using Terminar.Modules.Identity.Infrastructure.Services;
using Terminar.SharedKernel;

namespace Terminar.Modules.Identity.Application.Auth.Login;

public sealed class LoginCommandHandler(
    UserManager<AppIdentityUser> userManager,
    SignInManager<AppIdentityUser> signInManager,
    JwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.Username)
            ?? throw new ForbiddenException("Invalid credentials.");

        if (!user.IsActive)
            throw new ForbiddenException("Account is deactivated.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            throw new ForbiddenException("Account is locked. Please try again later.");

        if (!result.Succeeded)
            throw new ForbiddenException("Invalid credentials.");

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(user);

        return new LoginResult(accessToken, refreshToken, ExpiresIn: 3600);
    }
}
