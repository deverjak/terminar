using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain;

public sealed class TenantPluginActivation
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; } = default!;
    public string PluginId { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public DateTime? DisabledAt { get; private set; }

    private TenantPluginActivation() { }

    public static TenantPluginActivation Create(TenantId tenantId, string pluginId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        if (pluginId.Length > 64)
            throw new ArgumentException("Plugin ID must not exceed 64 characters.", nameof(pluginId));

        return new TenantPluginActivation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PluginId = pluginId,
            IsEnabled = false
        };
    }

    public void Enable()
    {
        IsEnabled = true;
        EnabledAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        DisabledAt = DateTime.UtcNow;
    }
}
