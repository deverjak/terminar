# Quickstart: Adding a New Plugin

**Feature**: 007-plugin-architecture  
**Date**: 2026-04-04

This guide walks a maintainer through creating a new plugin (e.g., "Payments") end-to-end.

---

## 1. Define the plugin ID

Choose a stable lowercase slug. Once tenants activate the plugin, this ID must never change.

```
payments
```

---

## 2. Backend: Implement ITerminarPlugin

Create a plugin class in `Terminar.Api` (or the relevant module):

```csharp
// src/Terminar.Api/Plugins/PaymentsPlugin.cs
public sealed class PaymentsPlugin : ITerminarPlugin
{
    public PluginDescriptor Descriptor => new(
        Id: "payments",
        Name: "Payments",
        Description: "Collect payment for course registrations.");

    public void RegisterServices(IServiceCollection services)
    {
        // Register any DI services specific to this plugin (if not already in AddXxxModule)
    }

    public void MapEndpoints(IEndpointRouteBuilder app, IEndpointFilter pluginGuardFilter)
    {
        var group = app.MapGroup("/api/v1/payments")
                       .AddEndpointFilter(pluginGuardFilter)
                       .RequireAuthorization("StaffOrAdmin")
                       .WithTags("Payments");

        // group.MapGet(...), group.MapPost(...), etc.
    }
}
```

---

## 3. Backend: Register the plugin

In `src/Terminar.Api/Program.cs`, add the plugin to the catalog:

```csharp
builder.Services.AddTerminarPlugin<PaymentsPlugin>();
// (This registers ITerminarPlugin and the plugin's services)
```

Then wire up endpoints after `app.Build()`:

```csharp
var pluginGuardFactory = app.Services.GetRequiredService<IPluginGuardFilterFactory>();
foreach (var plugin in app.Services.GetServices<ITerminarPlugin>())
{
    plugin.MapEndpoints(app, pluginGuardFactory.CreateFor(plugin.Descriptor.Id));
}
```

---

## 4. Frontend: Create the plugin contribution file

```typescript
// frontend/src/features/payments/plugin.ts
import { IconCreditCard } from '@tabler/icons-react';
import type { PluginContribution } from '@/shared/plugins/types';

export const paymentsPluginContribution: PluginContribution = {
  id: 'payments',
  routes: [
    {
      path: 'payments',
      lazy: () => import('./PaymentsPage').then(m => ({ Component: m.PaymentsPage })),
    },
  ],
  navItems: [
    {
      path: '/app/payments',
      labelKey: 'nav.payments',
      icon: IconCreditCard,
    },
  ],
};
```

---

## 5. Frontend: Register in the plugin registry

```typescript
// frontend/src/shared/plugins/pluginRegistry.ts
import { paymentsPluginContribution } from '@/features/payments/plugin';

const PLUGIN_REGISTRY: Record<string, PluginContribution> = {
  excusals: excusalPluginContribution,
  payments: paymentsPluginContribution, // <-- add this line
};
```

---

## 6. Frontend: Add i18n keys

Add the nav label in both locale files:

```json
// frontend/src/shared/i18n/locales/en.json
{
  "nav": {
    "payments": "Payments"
  }
}

// frontend/src/shared/i18n/locales/cs.json
{
  "nav": {
    "payments": "Platby"
  }
}
```

---

## 7. Verify

1. Start the application: `cd src/Terminar.AppHost && dotnet run`
2. Open Settings → Plugins — the Payments plugin should appear (inactive).
3. Activate it — the Payments nav item and route become available.
4. Deactivate it — the nav item disappears and `/app/payments` redirects to 404.
5. The backend `/api/v1/payments/*` routes return `422 plugin_not_enabled` when inactive.

---

## Checklist for new plugins

- [ ] Plugin ID is lowercase, hyphen-separated, and unique
- [ ] `ITerminarPlugin` implemented and registered in `Program.cs`
- [ ] All plugin endpoints grouped under `pluginGuardFilter`
- [ ] Frontend `plugin.ts` created with routes and nav items
- [ ] Frontend registry updated in `pluginRegistry.ts`
- [ ] i18n keys added to both `en.json` and `cs.json`
- [ ] Plugin appears in Settings → Plugins page
- [ ] Routes are inaccessible when plugin is inactive
