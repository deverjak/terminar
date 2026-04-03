using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.UpdateExcusalCredit;

public sealed record UpdateExcusalCreditCommand(
    Guid CreditId,
    Guid TenantId,
    Guid ActorStaffId,
    List<Guid>? AdditionalWindowIds,
    List<string>? NewTags) : IRequest;
