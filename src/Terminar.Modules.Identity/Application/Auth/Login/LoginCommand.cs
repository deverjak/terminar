using MediatR;

namespace Terminar.Modules.Identity.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(string AccessToken, string RefreshToken, int ExpiresIn, string TenantSlug, string TokenType = "Bearer");
