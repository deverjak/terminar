# Research: Plugin Architecture for Tenant Features

**Feature**: 007-plugin-architecture  
**Date**: 2026-04-04

---

## Decision 1: Plugin Catalog Storage

**Decision**: In-memory registry built from DI-registered `ITerminarPlugin` instances at startup.

**Rationale**: All plugins are built by maintainers and compiled into the application — there is no need for runtime discovery from disk, database, or external sources. An in-memory catalog is simpler, type-safe, and always in sync with deployed code.

**Alternatives considered**:
- Database-backed catalog table: Rejected — adds schema maintenance burden for data that never changes between deployments.
- File-based plugin discovery (like MEF/Roslyn): Rejected — over-engineering for an internal maintainer-only plugin system; no third-party isolation is needed.

---

## Decision 2: Plugin Activation Persistence

**Decision**: A dedicated `tenants.tenant_plugin_activations` table with a `TenantPluginActivation` entity in `Terminar.Modules.Tenants`, owned by (but not embedded in) the `Tenant` aggregate.

**Rationale**: Per-tenant plugin state must survive restarts and must be scoped to a tenant, which aligns with the Tenants bounded context. Storing it as a separate table (rather than a JSON column on Tenants) allows straightforward querying ("is plugin X enabled for tenant Y?") without loading the full Tenant aggregate.

**Alternatives considered**:
- JSON column on the Tenant row: Simpler but makes querying plugin state awkward and couples plugin IDs to the Tenant aggregate.
- Embedded collection in Tenant aggregate: Possible, but loading all activation records every time Tenant is fetched adds unnecessary overhead as the plugin count grows.

---

## Decision 3: Backend Feature Guard Mechanism

**Decision**: An ASP.NET Core endpoint filter (`PluginGuardFilter`) applied to endpoint groups registered per plugin. Each plugin's `MapEndpoints` method receives a tagged route group, and the filter checks `TenantPluginActivation` before the handler runs.

**Rationale**: Applying the guard at the HTTP/API boundary is the most explicit and consistent layer — it intercepts before MediatR dispatch, returns HTTP 422 with a structured error body, and keeps plugin guard logic out of application/domain code. The filter reads the active plugin list from a per-request cache (populated by a middleware using the tenant context).

**Alternatives considered**:
- MediatR pipeline behavior with `[RequiresPlugin]` attribute on handler classes: Valid, but the guard fires after request parsing and model binding, and the error response format requires more wiring than a simple endpoint filter.
- Middleware that short-circuits based on route prefix: Fragile — depends on URL conventions matching plugin boundaries; breaks if routes are reorganized.

---

## Decision 4: Plugin Structural Boundary (No New Assembly)

**Decision**: No new C# project is created for the plugin system. `ITerminarPlugin` and `PluginId` live in `Terminar.SharedKernel`. Excusal-related domain code stays in its existing modules (`Terminar.Modules.Registrations`, `Terminar.Modules.Tenants`, `Terminar.Modules.Courses`). The plugin boundary is enforced by the activation guard and the plugin registration convention, not by physical assembly separation.

**Rationale**: The user explicitly stated that plugins are maintainer-built and can access the DB directly (WordPress-style). Physical assembly isolation would add project management overhead with no practical benefit. The architectural boundary is logical (guard + registration contract), not physical.

**Alternatives considered**:
- New `Terminar.Plugins.Excusals` project: Rejected — unnecessary indirection; the excusal domain code already lives in appropriately named modules.
- emdash-style event-driven isolation: Explicitly rejected by user; over-engineering for an internal team.

---

## Decision 5: Frontend Plugin Integration

**Decision**: A `useActivePlugins()` hook backed by TanStack Query that fetches `GET /api/v1/settings/plugins`. A static frontend plugin registry maps `pluginId → { routes: RouteObject[], navItems: NavItem[] }`. `AppShellLayout` and the router consume active plugin lists to conditionally render nav items and register routes.

**Rationale**: Fetching active plugins once at app load (with caching) and using the result to drive both routing and navigation is the minimal-overhead approach. The static frontend registry keeps plugin UI contributions co-located with the plugin's feature folder, rather than scattered across the router and layout.

**Alternatives considered**:
- Hardcoded `if (pluginEnabled) { ... }` checks throughout: Current state — does not scale to more plugins.
- Dynamic route code-splitting per plugin (lazy imports conditioned on active plugins): Possible future enhancement but unnecessary for the initial scope; all plugin code is compiled in regardless.

---

## Decision 6: Excusal Plugin Auto-Migration

**Decision**: A one-time data migration (EF Core `Sql()` call within the Tenants migration) that inserts `tenant_plugin_activations` rows for the `excusals` plugin for every tenant that has non-default `ExcusalSettings` or any `ExcusalValidityWindow` records.

**Rationale**: Existing tenants using excusals must not lose access after deployment. A data migration is the safest way to ensure this — it runs atomically with the schema migration and is repeatable on any environment.

**Alternatives considered**:
- Application startup code that seeds activations: Runs outside migrations, harder to reason about ordering, and can double-insert.
- Manual admin action: Rejected — spec requires zero manual intervention.

---

## Decision 7: Frontend Plugin Registry Location

**Decision**: A new file `frontend/src/shared/plugins/pluginRegistry.ts` that exports a static map of `pluginId → PluginContribution`. Each feature folder contributing a plugin exports its contribution (routes, nav items) and registers it in this central file.

**Rationale**: Central registry is easy to find and extend. Alternatives like auto-discovery (scanning feature folders) add build complexity. Since the number of plugins is small and all are internal, a manual registry is appropriate.

---

## Resolved Unknowns

| Unknown | Resolution |
|---------|-----------|
| Should plugins have their own DbContext? | No — they share existing module DbContexts (RegistrationsDbContext, TenantsDbContext, etc.) |
| Should `ITerminarPlugin` enforce DI registration conventions? | Yes — it has a `RegisterServices(IServiceCollection)` method and a `MapEndpoints(IEndpointRouteBuilder)` method |
| How does the frontend know which plugins exist vs. which are active? | Backend returns all registered plugin descriptors with their activation status in one response |
| Payments plugin implementation depth? | Stub only: registered with name/description/id, no business logic, activatable/deactivatable, used to prove the registration convention works |
