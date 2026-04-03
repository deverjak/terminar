using MediatR;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Registrations.Application.Commands.RedeemMagicLink;

public sealed class RedeemMagicLinkCommandHandler(IParticipantMagicLinkRepository repo)
    : IRequestHandler<RedeemMagicLinkCommand, RedeemMagicLinkResult>
{
    public async Task<RedeemMagicLinkResult> Handle(RedeemMagicLinkCommand request, CancellationToken cancellationToken)
    {
        var magicLink = await repo.GetByMagicLinkTokenAsync(request.Token, cancellationToken);

        if (magicLink is null || !magicLink.IsMagicLinkValid)
            throw new UnprocessableException("Magic link is invalid, expired, or already used.");

        var portalToken = magicLink.Redeem();
        await repo.SaveChangesAsync(cancellationToken);

        return new RedeemMagicLinkResult(portalToken, magicLink.PortalTokenExpiresAt!.Value);
    }
}
