namespace Terminar.SharedKernel.Plugins;

public interface IPluginCatalog
{
    bool IsRegistered(string pluginId);
    IReadOnlyList<PluginDescriptor> GetAll();
}
