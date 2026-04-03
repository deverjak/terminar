using MediatR;

namespace Terminar.Modules.Registrations.Application.Commands.SoftDeleteExcusalCredit;

public sealed record SoftDeleteExcusalCreditCommand(Guid CreditId, Guid TenantId, Guid ActorStaffId) : IRequest;
