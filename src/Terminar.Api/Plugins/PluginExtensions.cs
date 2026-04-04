using Terminar.SharedKernel.Plugins;

namespace Terminar.Api.Plugins;

public static class PluginExtensions
{
    public static IServiceCollection AddTerminarPlugin<TPlugin>(this IServiceCollection services)
        where TPlugin : class, ITerminarPlugin
    {
        services.AddSingleton<ITerminarPlugin, TPlugin>();
        return services;
    }

    public static WebApplication UseTerminarPlugins(this WebApplication app)
    {
        var catalog = app.Services.GetRequiredService<IPluginCatalog>() as InMemoryPluginCatalog
            ?? throw new InvalidOperationException("IPluginCatalog must be InMemoryPluginCatalog to register plugins.");

        var factory = app.Services.GetRequiredService<PluginGuardFilterFactory>();
        var plugins = app.Services.GetServices<ITerminarPlugin>();

        foreach (var plugin in plugins)
        {
            catalog.Register(plugin.Descriptor);
            var guardFilter = factory.CreateFor(plugin.Descriptor.Id);
            plugin.MapEndpoints(app, guardFilter);
        }

        return app;
    }
}
