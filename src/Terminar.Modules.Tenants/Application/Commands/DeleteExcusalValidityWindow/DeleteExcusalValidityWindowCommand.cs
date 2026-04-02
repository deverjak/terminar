using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.DeleteExcusalValidityWindow;

public sealed record DeleteExcusalValidityWindowCommand(Guid WindowId, Guid TenantId) : IRequest;
