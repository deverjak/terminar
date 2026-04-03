using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.UpdateExcusalValidityWindow;

public sealed record UpdateExcusalValidityWindowCommand(
    Guid WindowId,
    Guid TenantId,
    string? Name,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest;
