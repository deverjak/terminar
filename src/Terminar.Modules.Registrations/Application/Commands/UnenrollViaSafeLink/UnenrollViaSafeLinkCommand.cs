using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.UnenrollViaSafeLink;

public sealed record UnenrollViaSafeLinkCommand(Guid SafeLinkToken, Guid TenantId) : IRequest;
