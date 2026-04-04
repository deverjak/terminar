using Terminar.SharedKernel.Plugins;

namespace Terminar.Api.Plugins;

public sealed class PaymentsPlugin : ITerminarPlugin
{
    public PluginDescriptor Descriptor => new(
        "payments",
        "Payments",
        "Collect payment for course registrations.");

    public void RegisterServices(IServiceCollection services)
    {
        // Stub — no services to register yet
    }

    public void MapEndpoints(IEndpointRouteBuilder app, IEndpointFilter pluginGuardFilter)
    {
        var group = app.MapGroup("/api/v1/payments")
            .AddEndpointFilter(pluginGuardFilter)
            .RequireAuthorization("StaffOrAdmin")
            .WithTags("Payments");

        group.MapGet("/status", () => Results.Ok(new { status = "payments_stub" }));
    }
}
