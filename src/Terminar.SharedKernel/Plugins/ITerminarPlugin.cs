using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Terminar.SharedKernel.Plugins;

public interface ITerminarPlugin
{
    PluginDescriptor Descriptor { get; }
    void RegisterServices(IServiceCollection services);
    void MapEndpoints(IEndpointRouteBuilder app, IEndpointFilter pluginGuardFilter);
}
