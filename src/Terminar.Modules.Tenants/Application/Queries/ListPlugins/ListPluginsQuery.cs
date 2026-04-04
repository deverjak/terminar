using MediatR;

namespace Terminar.Modules.Tenants.Application.Queries.ListPlugins;

public sealed record PluginStatusDto(string Id, string Name, string Description, bool IsEnabled);

public sealed record ListPluginsQuery(Guid TenantId) : IRequest<IReadOnlyList<PluginStatusDto>>;
