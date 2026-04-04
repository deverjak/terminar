# Implementation Plan: Plugin Architecture for Tenant Features

**Branch**: `007-plugin-architecture` | **Date**: 2026-04-04 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/007-plugin-architecture/spec.md`

**Additional context from `/speckit.plan`**: Plugin mechanism must work on frontend for dynamic UI; plugins can access DB directly (WordPress-style, no emdash isolation); plugins are maintainer-built only.

## Summary

Introduce a lightweight plugin architecture that allows optional features (starting with Excusals) to be activated or deactivated per tenant by an administrator. The system provides: (1) a backend `ITerminarPlugin` contract in SharedKernel for plugin registration and endpoint contribution, (2) a `TenantPluginActivation` entity in the Tenants module tracking per-tenant activation state, (3) an endpoint filter guard applied to all plugin-contributed routes, and (4) a frontend plugin registry driving dynamic nav items and routes via `useActivePlugins()`. No new .NET project or physical assembly isolation is needed — all plugins share the existing module infrastructure and DbContexts.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql) | Mantine v9, TanStack Query v5, react-i18next, React Router v7  
**Storage**: PostgreSQL via EF Core — **1 new table** with **1 new migration** in `Terminar.Modules.Tenants` + 1 data migration to auto-enable excusals for existing tenants  
**Testing**: `dotnet test` (xUnit); existing integration tests in `Terminar.Api.IntegrationTests`  
**Target Platform**: Web service (ASP.NET Core) + Web SPA (React/Vite)  
**Project Type**: Web application (backend API + frontend SPA)  
**Performance Goals**: Plugin activation check must add <5ms to any API request (single DB read, cacheable per request)  
**Constraints**: Zero behavioral regression for existing tenants; no new .NET project; plugins access shared DB directly (no event-based isolation)  
**Scale/Scope**: Small plugin catalog (2–10 plugins); per-tenant activation; all plugins are internal maintainer code

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I — Domain-Driven Design ✅

- `TenantPluginActivation` is a domain entity in `Terminar.Modules.Tenants` with a proper repository interface.
- `PluginDescriptor` and `ITerminarPlugin` are value/interface types in SharedKernel.
- `Tenant` aggregate raises `TenantPluginEnabled`/`TenantPluginDisabled` domain events.
- Business logic (guard check, activation state change) stays in domain/application layers; infrastructure implements repositories.

### Principle II — Multi-Tenancy by Default ✅

- `TenantPluginActivation` is scoped by `TenantId`. Plugin state for one tenant cannot affect another.
- All plugin guard checks use `ITenantContext.TenantId` from the resolved request context.

### Principle III — Multi-Language First ✅

- Plugin `Name` and `Description` (displayed in Settings UI) are hardcoded in English in the backend descriptor; the frontend uses i18n keys for nav labels. Plugin display names would be i18n-keyed in the settings page (name fetched from API, description from i18n key).
- No user-facing strings are hardcoded outside translation files.

### Principle IV — Clean Architecture ✅ (with clarification)

- `ITerminarPlugin` and `PluginDescriptor` live in SharedKernel — zero external dependencies.
- Repository interface `ITenantPluginActivationRepository` is in Domain layer.
- EF Core implementation in Infrastructure layer.
- **Clarification on "direct DB access"**: Plugins share existing module DbContexts (e.g., `RegistrationsDbContext` for excusal entities). This is consistent with the existing architecture — each module already directly accesses its own tables. The "WordPress-style direct access" means no event-driven cross-module isolation, not a bypass of the layered architecture within each module.

### Principle V — Spec-First Development ✅

- `spec.md` approved. This plan derives from it.

## Project Structure

### Documentation (this feature)

```text
specs/007-plugin-architecture/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   ├── backend-plugin-contract.md   ← Phase 1 output
│   └── frontend-plugin-contract.md  ← Phase 1 output
└── tasks.md             ← Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
  Terminar.SharedKernel/
    Plugins/
      ITerminarPlugin.cs            ← NEW: plugin registration contract
      PluginDescriptor.cs           ← NEW: value object (Id, Name, Description)

  Terminar.Modules.Tenants/
    Domain/
      TenantPluginActivation.cs     ← NEW: entity
      Events/
        TenantPluginEnabled.cs      ← NEW: domain event
        TenantPluginDisabled.cs     ← NEW: domain event
      Repositories/
        ITenantPluginActivationRepository.cs  ← NEW: repository interface
    Application/
      Commands/
        EnablePlugin/
          EnablePluginCommand.cs           ← NEW
          EnablePluginCommandHandler.cs    ← NEW
        DisablePlugin/
          DisablePluginCommand.cs          ← NEW
          DisablePluginCommandHandler.cs   ← NEW
      Queries/
        ListPlugins/
          ListPluginsQuery.cs              ← NEW
          ListPluginsQueryHandler.cs       ← NEW
    Infrastructure/
      Repositories/
        TenantPluginActivationRepository.cs  ← NEW
      Migrations/
        YYYYMMDDHHMMSS_AddTenantPluginActivations.cs  ← NEW (schema + data migration)
      TenantsDbContext.cs            ← MODIFIED: add DbSet<TenantPluginActivation>

  Terminar.Api/
    Plugins/
      ExcusalPlugin.cs              ← NEW: excusal plugin registration
      PaymentsPlugin.cs             ← NEW: stub payment plugin registration
      PluginGuardFilter.cs          ← NEW: IEndpointFilter checking activation
      IPluginGuardFilterFactory.cs  ← NEW: factory to create filter per plugin ID
      PluginCatalog.cs              ← NEW: IPluginCatalog service (in-memory)
    Modules/
      PluginsModule.cs              ← NEW: GET/POST/DELETE /api/v1/settings/plugins/*
    [existing excusal modules]      ← MODIFIED: moved into ExcusalPlugin.MapEndpoints

frontend/
  src/
    shared/
      plugins/
        types.ts                    ← NEW: PluginContribution, NavItem, PluginStatus
        pluginRegistry.ts           ← NEW: static registry map
        pluginsApi.ts               ← NEW: fetch/activate/deactivate API calls
        useActivePlugins.ts         ← NEW: TanStack Query hook
    features/
      plugins/
        PluginsSettingsPage.tsx     ← NEW: admin UI for plugin management
      excusal-credits/
        plugin.ts                   ← NEW: excusal plugin contribution
      payments/
        plugin.ts                   ← NEW: payments stub contribution
    app/
      router.tsx                    ← MODIFIED: dynamic plugin routes
    shared/
      components/
        AppShellLayout.tsx          ← MODIFIED: dynamic nav items from plugins
    shared/
      i18n/locales/
        en.json                     ← MODIFIED: add plugin UI strings
        cs.json                     ← MODIFIED: add plugin UI strings
```

**Structure Decision**: Web application with DDD-modular backend. No new .NET project introduced. Plugin infrastructure code lives in SharedKernel (contract) and Terminar.Api (orchestration). Module-specific plugin logic (endpoint wiring) stays in Terminar.Api plugin files. Frontend uses a new `shared/plugins/` folder for the registry and hooks.

## Complexity Tracking

No constitution violations to justify. All additions fit within existing module and layer boundaries.

---

## Implementation Phases

### Phase 1: Backend Plugin Infrastructure

**Goal**: `ITerminarPlugin` contract, plugin catalog, activation persistence, guard filter, and management API.

**Order** (respects DDD dependency direction):

1. SharedKernel: `ITerminarPlugin`, `PluginDescriptor`
2. Tenants Domain: `TenantPluginActivation` entity, `ITenantPluginActivationRepository`, domain events
3. Tenants Application: `EnablePlugin`, `DisablePlugin`, `ListPlugins` commands/queries
4. Tenants Infrastructure: EF Core entity config, migration (schema + data seed), repository implementation
5. Api: `IPluginCatalog`, `PluginGuardFilter`, `IPluginGuardFilterFactory`, `PluginsModule` endpoints
6. Api: `ExcusalPlugin` class — moves all excusal endpoint wiring from current modules into plugin class with guard
7. Api: `PaymentsPlugin` stub — proves the extension point

### Phase 2: Frontend Plugin System

**Goal**: Dynamic nav items, dynamic routes, plugin settings page, excusal routes migrated.

**Order**:

1. `shared/plugins/types.ts`, `pluginsApi.ts`, `useActivePlugins.ts`
2. `shared/plugins/pluginRegistry.ts` (empty registry first, then populated)
3. `features/excusal-credits/plugin.ts` + `features/payments/plugin.ts`
4. `PluginsSettingsPage.tsx`
5. `AppShellLayout.tsx` — replace hardcoded excusal nav with dynamic plugin nav
6. `router.tsx` — replace hardcoded excusal routes with dynamic plugin routes
7. i18n keys for plugin UI

### Phase 3: Integration & Migration Verification

1. Run migrations against local dev DB, verify auto-seed of excusals plugin for existing tenants
2. Verify all existing excusal workflows pass with plugin active
3. Verify excusal endpoints return 422 with plugin inactive
4. Verify payments stub appears in plugin list but its endpoints/routes are absent/blocked
5. Verify plugin activation/deactivation from Settings UI reflects immediately on nav and route access
