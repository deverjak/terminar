using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.CreateExcusal;

public sealed record CreateExcusalCommand(
    Guid SafeLinkToken,
    Guid SessionId,
    Guid TenantId) : IRequest<CreateExcusalResult>;

public sealed record CreateExcusalResult(Guid ExcusalId, bool CreditIssued);
