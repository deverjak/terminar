using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.DisablePlugin;

public sealed record DisablePluginCommand(Guid TenantId, string PluginId) : IRequest;
