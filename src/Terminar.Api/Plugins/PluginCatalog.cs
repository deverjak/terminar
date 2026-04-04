using System.Collections.Concurrent;
using Terminar.SharedKernel.Plugins;

namespace Terminar.Api.Plugins;

public sealed class InMemoryPluginCatalog : IPluginCatalog
{
    private readonly ConcurrentDictionary<string, PluginDescriptor> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public void Register(PluginDescriptor descriptor)
        => _plugins[descriptor.Id] = descriptor;

    public bool IsRegistered(string pluginId)
        => _plugins.ContainsKey(pluginId);

    public IReadOnlyList<PluginDescriptor> GetAll()
        => [.. _plugins.Values];
}
