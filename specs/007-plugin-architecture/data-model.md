# Data Model: Plugin Architecture for Tenant Features

**Feature**: 007-plugin-architecture  
**Date**: 2026-04-04

---

## New Entities

### TenantPluginActivation (Terminar.Modules.Tenants)

Records whether a specific plugin is enabled or disabled for a given tenant.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `TenantPluginActivationId` (Guid) | PK, strongly typed | StronglyTypedId |
| `TenantId` | `TenantId` (Guid) | FK → tenants.tenants, NOT NULL | Multi-tenancy scope |
| `PluginId` | `string` | NOT NULL, max 64 chars | Slug e.g. `"excusals"`, `"payments"` |
| `IsEnabled` | `bool` | NOT NULL, default `false` | Activation state |
| `EnabledAt` | `DateTime?` | nullable | UTC timestamp when last activated |
| `DisabledAt` | `DateTime?` | nullable | UTC timestamp when last deactivated |

**Table**: `tenants.tenant_plugin_activations`  
**Unique constraint**: `(TenantId, PluginId)` — one record per plugin per tenant  
**Index**: `(TenantId)` for fast per-tenant plugin lookups

**State transitions**:
- Created (disabled) → Enabled: `IsEnabled = true`, `EnabledAt = now`
- Enabled → Disabled: `IsEnabled = false`, `DisabledAt = now`
- Re-enable: `IsEnabled = true`, `EnabledAt = now` (overwrites previous)

---

## Modified Entities

### Tenant (Terminar.Modules.Tenants)

No structural changes to the aggregate root or its persistence. Plugin activations are managed via a separate repository, not as a collection owned by Tenant. The `EnablePlugin` / `DisablePlugin` methods raise domain events but delegate persistence to `ITenantPluginActivationRepository`.

**New domain events raised by Tenant**:
- `TenantPluginEnabled(TenantId, PluginId)`
- `TenantPluginDisabled(TenantId, PluginId)`

---

## New Value Objects / Domain Concepts

### PluginDescriptor (Terminar.SharedKernel)

A read-only descriptor registered by each plugin at startup. Not persisted — lives in the in-memory plugin catalog.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` | Unique slug, e.g. `"excusals"` |
| `Name` | `string` | Display name, e.g. `"Excusals"` |
| `Description` | `string` | Short description for the settings UI |

### ITerminarPlugin (Terminar.SharedKernel)

The registration contract implemented by every plugin. Not a persisted entity.

```
interface ITerminarPlugin {
  PluginDescriptor Descriptor { get; }
  void RegisterServices(IServiceCollection services);
  void MapEndpoints(IEndpointRouteBuilder app);
}
```

---

## Schema Summary

### New table: `tenants.tenant_plugin_activations`

```sql
CREATE TABLE tenants.tenant_plugin_activations (
    id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id   UUID        NOT NULL REFERENCES tenants.tenants(id),
    plugin_id   VARCHAR(64) NOT NULL,
    is_enabled  BOOLEAN     NOT NULL DEFAULT FALSE,
    enabled_at  TIMESTAMPTZ,
    disabled_at TIMESTAMPTZ,
    CONSTRAINT uq_tenant_plugin UNIQUE (tenant_id, plugin_id)
);
CREATE INDEX ix_tenant_plugin_activations_tenant_id
    ON tenants.tenant_plugin_activations (tenant_id);
```

### Data migration (seeded with schema migration)

```sql
-- Auto-activate excusals plugin for all tenants that have existing excusal data
INSERT INTO tenants.tenant_plugin_activations (id, tenant_id, plugin_id, is_enabled, enabled_at)
SELECT gen_random_uuid(), t.id, 'excusals', TRUE, NOW()
FROM   tenants.tenants t
WHERE  EXISTS (
    SELECT 1 FROM tenants.excusal_validity_windows w WHERE w.tenant_id = t.id
)
ON CONFLICT (tenant_id, plugin_id) DO NOTHING;
```

---

## Frontend Data Structures

### PluginStatus (API response shape)

```typescript
interface PluginStatus {
  id: string;          // e.g. "excusals"
  name: string;        // e.g. "Excusals"
  description: string;
  isEnabled: boolean;
}
```

### PluginContribution (frontend registry)

```typescript
interface PluginContribution {
  routes: RouteObject[];       // React Router route objects
  navItems: NavItem[];         // Sidebar nav link definitions
}

interface NavItem {
  path: string;
  labelKey: string;            // i18n key
  icon: React.ComponentType;
  requiredRole?: 'Admin' | 'Staff';
}
```
