using MediatR;
using Terminar.Api.Middleware;
using Terminar.Modules.Tenants.Application.Commands.DisablePlugin;
using Terminar.Modules.Tenants.Application.Commands.EnablePlugin;
using Terminar.Modules.Tenants.Application.Queries.ListPlugins;

namespace Terminar.Api.Modules;

public static class PluginsModule
{
    public static IEndpointRouteBuilder MapPluginsEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/settings/plugins
        app.MapGet("/api/v1/settings/plugins", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new ListPluginsQuery(tenantId.Value), ct);
            return Results.Ok(result);
        }).RequireAuthorization("StaffOrAdmin").WithTags("Plugins");

        // POST /api/v1/settings/plugins/{pluginId}/activate
        app.MapPost("/api/v1/settings/plugins/{pluginId}/activate", async (
            string pluginId,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            try
            {
                await mediator.Send(new EnablePluginCommand(tenantId.Value, pluginId), ct);
                return Results.Ok();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound(new { error = "plugin_not_found", plugin_id = pluginId });
            }
        }).RequireAuthorization("AdminOnly").WithTags("Plugins");

        // DELETE /api/v1/settings/plugins/{pluginId}/activate
        app.MapDelete("/api/v1/settings/plugins/{pluginId}/activate", async (
            string pluginId,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            try
            {
                await mediator.Send(new DisablePluginCommand(tenantId.Value, pluginId), ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound(new { error = "plugin_not_found", plugin_id = pluginId });
            }
        }).RequireAuthorization("AdminOnly").WithTags("Plugins");

        return app;
    }
}
