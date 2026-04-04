using MediatR;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Events;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.Plugins;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Commands.EnablePlugin;

public sealed class EnablePluginCommandHandler(
    IPluginCatalog pluginCatalog,
    ITenantPluginActivationRepository repo,
    IMediator mediator)
    : IRequestHandler<EnablePluginCommand>
{
    public async Task Handle(EnablePluginCommand request, CancellationToken cancellationToken)
    {
        if (!pluginCatalog.IsRegistered(request.PluginId))
            throw new InvalidOperationException($"Plugin '{request.PluginId}' is not registered.");

        var tenantId = TenantId.From(request.TenantId);
        var activation = await repo.FindAsync(tenantId, request.PluginId, cancellationToken);

        if (activation is null)
        {
            activation = TenantPluginActivation.Create(tenantId, request.PluginId);
            await repo.AddAsync(activation, cancellationToken);
        }

        activation.Enable();
        await repo.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new TenantPluginEnabled(Guid.NewGuid(), DateTime.UtcNow, tenantId, request.PluginId), cancellationToken);
    }
}
