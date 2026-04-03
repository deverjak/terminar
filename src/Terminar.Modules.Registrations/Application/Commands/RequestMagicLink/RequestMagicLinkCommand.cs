using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.RequestMagicLink;

public sealed record RequestMagicLinkCommand(Guid TenantId, string Email) : IRequest<RequestMagicLinkResult>;
public sealed record RequestMagicLinkResult(string MagicLinkToken, bool WasSent);
