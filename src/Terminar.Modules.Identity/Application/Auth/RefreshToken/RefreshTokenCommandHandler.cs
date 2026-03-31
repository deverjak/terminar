using MediatR;
using Terminar.Modules.Identity.Infrastructure.Services;
using Terminar.SharedKernel;

namespace Terminar.Modules.Identity.Application.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler(JwtTokenService jwtTokenService)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await jwtTokenService.ValidateRefreshTokenAsync(request.UserId, request.RefreshToken)
            ?? throw new ForbiddenException("Invalid or expired refresh token.");

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = await jwtTokenService.GenerateRefreshTokenAsync(user);

        return new RefreshTokenResult(accessToken, newRefreshToken, ExpiresIn: 3600);
    }
}
