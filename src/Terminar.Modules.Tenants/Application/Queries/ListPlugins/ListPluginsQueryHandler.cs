using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.Plugins;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Queries.ListPlugins;

public sealed class ListPluginsQueryHandler(
    IPluginCatalog pluginCatalog,
    ITenantPluginActivationRepository repo)
    : IRequestHandler<ListPluginsQuery, IReadOnlyList<PluginStatusDto>>
{
    public async Task<IReadOnlyList<PluginStatusDto>> Handle(ListPluginsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var activations = await repo.ListForTenantAsync(tenantId, cancellationToken);
        var activationMap = activations.ToDictionary(a => a.PluginId, a => a.IsEnabled);

        return pluginCatalog.GetAll()
            .Select(d => new PluginStatusDto(
                d.Id,
                d.Name,
                d.Description,
                activationMap.TryGetValue(d.Id, out var enabled) && enabled))
            .ToList();
    }
}
