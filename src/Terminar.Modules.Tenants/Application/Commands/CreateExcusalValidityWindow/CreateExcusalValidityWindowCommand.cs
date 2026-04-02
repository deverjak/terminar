using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.CreateExcusalValidityWindow;

public sealed record CreateExcusalValidityWindowCommand(
    Guid TenantId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<Guid>;
