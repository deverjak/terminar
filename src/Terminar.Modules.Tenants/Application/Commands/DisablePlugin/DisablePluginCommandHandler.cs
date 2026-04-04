using MediatR;
using Terminar.Modules.Tenants.Domain.Events;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.Plugins;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Commands.DisablePlugin;

public sealed class DisablePluginCommandHandler(
    IPluginCatalog pluginCatalog,
    ITenantPluginActivationRepository repo,
    IMediator mediator)
    : IRequestHandler<DisablePluginCommand>
{
    public async Task Handle(DisablePluginCommand request, CancellationToken cancellationToken)
    {
        if (!pluginCatalog.IsRegistered(request.PluginId))
            throw new InvalidOperationException($"Plugin '{request.PluginId}' is not registered.");

        var tenantId = TenantId.From(request.TenantId);
        var activation = await repo.FindAsync(tenantId, request.PluginId, cancellationToken);

        if (activation is null)
            return;

        activation.Disable();
        await repo.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new TenantPluginDisabled(Guid.NewGuid(), DateTime.UtcNow, tenantId, request.PluginId), cancellationToken);
    }
}
