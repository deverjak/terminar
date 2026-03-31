using MediatR;

namespace Terminar.Modules.Identity.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string UserId, string RefreshToken) : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");
