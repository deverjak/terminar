using MediatR;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.Commands.RequestMagicLink;

public sealed class RequestMagicLinkCommandHandler(
    IRegistrationRepository registrationRepo,
    IParticipantMagicLinkRepository magicLinkRepo)
    : IRequestHandler<RequestMagicLinkCommand, RequestMagicLinkResult>
{
    public async Task<RequestMagicLinkResult> Handle(RequestMagicLinkCommand request, CancellationToken cancellationToken)
    {
        // Always return success to prevent email enumeration
        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (ArgumentException)
        {
            return new RequestMagicLinkResult(string.Empty, false);
        }

        var hasRegistrations = await registrationRepo.HasActiveRegistrationsAsync(
            request.TenantId, email.Value, cancellationToken);

        if (!hasRegistrations)
            return new RequestMagicLinkResult(string.Empty, false);

        var tenantId = TenantId.From(request.TenantId);
        var magicLink = ParticipantMagicLink.Create(tenantId, email);
        await magicLinkRepo.AddAsync(magicLink, cancellationToken);
        await magicLinkRepo.SaveChangesAsync(cancellationToken);

        return new RequestMagicLinkResult(magicLink.MagicLinkToken, true);
    }
}
