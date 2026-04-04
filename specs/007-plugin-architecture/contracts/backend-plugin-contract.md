# Backend Plugin Contract

**Feature**: 007-plugin-architecture  
**Date**: 2026-04-04

---

## Plugin Registration Contract

Every plugin MUST implement `ITerminarPlugin` defined in `Terminar.SharedKernel`:

```csharp
// Terminar.SharedKernel/Plugins/ITerminarPlugin.cs
public interface ITerminarPlugin
{
    PluginDescriptor Descriptor { get; }
    void RegisterServices(IServiceCollection services);
    void MapEndpoints(IEndpointRouteBuilder app, IEndpointFilter pluginGuardFilter);
}

public sealed record PluginDescriptor(string Id, string Name, string Description);
```

### Plugin ID convention
- Lowercase, hyphen-separated slug: `"excusals"`, `"payments"`, `"custom-scheduling"`
- Must be stable — changing a plugin ID loses activation state for all tenants

### Registration pattern

Each plugin creates a class in its own module and registers it in `Terminar.Api/Program.cs`:

```csharp
// Example: ExcusalPlugin.cs (in Terminar.Api or Terminar.Modules.Registrations)
public sealed class ExcusalPlugin : ITerminarPlugin
{
    public PluginDescriptor Descriptor => new("excusals", "Excusals", "...");

    public void RegisterServices(IServiceCollection services)
    {
        // Any plugin-specific DI registrations beyond what the module already registers
        // Usually empty — module DI registration happens in AddXxxModule()
    }

    public void MapEndpoints(IEndpointRouteBuilder app, IEndpointFilter pluginGuardFilter)
    {
        var group = app.MapGroup("/api/v1")
                       .AddEndpointFilter(pluginGuardFilter)
                       .WithTags("Excusals");

        group.MapGet("/excusal-credits", ...);
        group.MapPost("/excusals", ...);
        // etc.
    }
}
```

---

## Plugin Catalog API

### GET /api/v1/settings/plugins

Returns all registered plugins with their activation state for the current tenant.

**Authorization**: `StaffOrAdmin`

**Response** `200 OK`:
```json
[
  {
    "id": "excusals",
    "name": "Excusals",
    "description": "Allows participants to be excused from courses and receive credits for future enrollment.",
    "isEnabled": true
  },
  {
    "id": "payments",
    "name": "Payments",
    "description": "Collect payment for course registrations.",
    "isEnabled": false
  }
]
```

---

### POST /api/v1/settings/plugins/{pluginId}/activate

Activates the plugin for the current tenant.

**Authorization**: `AdminOnly`  
**Path param**: `pluginId` — must match a registered plugin ID

**Response**: `200 OK` on success, `404 Not Found` if `pluginId` is not in the registry, `400 Bad Request` if already enabled.

---

### DELETE /api/v1/settings/plugins/{pluginId}/activate

Deactivates the plugin for the current tenant.

**Authorization**: `AdminOnly`  
**Path param**: `pluginId` — must match a registered plugin ID

**Response**: `204 No Content` on success, `404 Not Found` if not in registry, `400 Bad Request` if already disabled.

---

## Plugin Guard Filter

The `PluginGuardFilter` is an `IEndpointFilter` injected into the endpoint group by `MapEndpoints`:

- Reads `TenantId` from `ITenantContext`
- Looks up the plugin activation state via `IPluginActivationService` (thin query service over `TenantPluginActivationRepository`)
- If inactive: returns `Results.UnprocessableEntity(new { error = "plugin_not_enabled", pluginId = "..." })`
- If active: calls `next(ctx)`

The filter is **constructed per-plugin** (injected with the plugin ID), so each plugin's route group has its own guard instance.

---

## Plugin Activation Repository Interface

```csharp
// Terminar.Modules.Tenants/Domain/Repositories/ITenantPluginActivationRepository.cs
public interface ITenantPluginActivationRepository
{
    Task<bool> IsEnabledAsync(TenantId tenantId, string pluginId, CancellationToken ct);
    Task<IReadOnlyList<TenantPluginActivation>> ListForTenantAsync(TenantId tenantId, CancellationToken ct);
    Task<TenantPluginActivation?> FindAsync(TenantId tenantId, string pluginId, CancellationToken ct);
    Task AddAsync(TenantPluginActivation activation, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
```
