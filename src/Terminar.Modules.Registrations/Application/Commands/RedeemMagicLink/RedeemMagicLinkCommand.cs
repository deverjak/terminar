using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.RedeemMagicLink;

public sealed record RedeemMagicLinkCommand(string Token) : IRequest<RedeemMagicLinkResult>;
public sealed record RedeemMagicLinkResult(string PortalToken, DateTime ExpiresAt);
