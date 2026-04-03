using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.RedeemExcusalCredit;

public sealed record RedeemExcusalCreditCommand(
    Guid CreditId,
    Guid TargetCourseId,
    string PortalToken,
    Guid TenantId) : IRequest<RedeemExcusalCreditResult>;

public sealed record RedeemExcusalCreditResult(Guid NewEnrollmentId, Guid SafeLinkToken);
