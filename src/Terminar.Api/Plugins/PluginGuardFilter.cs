using Terminar.Api.Middleware;
using Terminar.Modules.Tenants.Domain.Repositories;

namespace Terminar.Api.Plugins;

public sealed class PluginGuardFilter(string pluginId) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var tenantCtx = services.GetRequiredService<ITenantContext>();

        if (!tenantCtx.IsResolved)
            return Results.Unauthorized();

        var repo = services.GetRequiredService<ITenantPluginActivationRepository>();
        var tenantId = tenantCtx.TenantId;
        var isEnabled = await repo.IsEnabledAsync(tenantId, pluginId, context.HttpContext.RequestAborted);

        if (!isEnabled)
            return Results.UnprocessableEntity(new { error = "plugin_not_enabled", plugin_id = pluginId });

        return await next(context);
    }
}

public sealed class PluginGuardFilterFactory
{
    public IEndpointFilter CreateFor(string pluginId) => new PluginGuardFilter(pluginId);
}
