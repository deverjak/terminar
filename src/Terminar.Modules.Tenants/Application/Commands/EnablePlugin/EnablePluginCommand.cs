using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.EnablePlugin;

public sealed record EnablePluginCommand(Guid TenantId, string PluginId) : IRequest;
