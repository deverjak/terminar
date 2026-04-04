# Tasks: Plugin Architecture for Tenant Features

**Input**: Design documents from `/specs/007-plugin-architecture/`  
**Branch**: `007-plugin-architecture`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No test tasks generated — not requested in spec. Verification steps are included as smoke-test tasks.

**Organization**: Tasks follow DDD dependency direction (Domain → Application → Infrastructure → API) within each phase, then user story priority order (P1 → P2 → P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)

---

## Phase 1: Setup

**Purpose**: Create the `Plugins/` folder structure in SharedKernel with the core plugin contract interfaces.

- [x] T001 Create `src/Terminar.SharedKernel/Plugins/` folder and add three files: `ITerminarPlugin.cs` (interface with `PluginDescriptor Descriptor`, `void RegisterServices(IServiceCollection)`, `void MapEndpoints(IEndpointRouteBuilder, IEndpointFilter)`) , `PluginDescriptor.cs` (sealed record with `string Id, string Name, string Description`), and `IPluginCatalog.cs` (interface with `bool IsRegistered(string pluginId)` and `IReadOnlyList<PluginDescriptor> GetAll()`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Backend plugin infrastructure — domain entity, application commands/queries, EF Core persistence, and API-layer plugin catalog + guard. All user story work is blocked until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

**Order within phase**: Domain (T002–T004) → Application (T005–T007) → Infrastructure (T008–T011) → API (T012–T014)

- [x] T002 Create `src/Terminar.Modules.Tenants/Domain/TenantPluginActivation.cs` — entity with `TenantPluginActivationId` (StronglyTypedId Guid), `TenantId`, `string PluginId` (max 64 chars), `bool IsEnabled`, `DateTime? EnabledAt`, `DateTime? DisabledAt`; add `TenantPluginActivationId` strongly-typed ID in the same file or alongside existing ID files in the Domain folder
- [x] T003 [P] Create `src/Terminar.Modules.Tenants/Domain/Events/TenantPluginEnabled.cs` — domain event record with `TenantId` and `string PluginId`
- [x] T004 [P] Create `src/Terminar.Modules.Tenants/Domain/Events/TenantPluginDisabled.cs` — domain event record with `TenantId` and `string PluginId`
- [x] T005 Create `src/Terminar.Modules.Tenants/Domain/Repositories/ITenantPluginActivationRepository.cs` — interface with `Task<bool> IsEnabledAsync(TenantId, string pluginId, CancellationToken)`, `Task<IReadOnlyList<TenantPluginActivation>> ListForTenantAsync(TenantId, CancellationToken)`, `Task<TenantPluginActivation?> FindAsync(TenantId, string pluginId, CancellationToken)`, `Task AddAsync(TenantPluginActivation, CancellationToken)`, `Task SaveChangesAsync(CancellationToken)`
- [x] T006 [P] Create `src/Terminar.Modules.Tenants/Application/Commands/EnablePlugin/EnablePluginCommand.cs` and `EnablePluginCommandHandler.cs` — command takes `Guid TenantId, string PluginId`; handler uses `IPluginCatalog` (validates plugin exists) and `ITenantPluginActivationRepository` (find-or-create activation record, set `IsEnabled = true`, `EnabledAt = DateTime.UtcNow`); throws `InvalidOperationException` if plugin ID not in catalog; raises `TenantPluginEnabled` domain event
- [x] T007 [P] Create `src/Terminar.Modules.Tenants/Application/Commands/DisablePlugin/DisablePluginCommand.cs` and `DisablePluginCommandHandler.cs` — same shape as T006 but sets `IsEnabled = false`, `DisabledAt = DateTime.UtcNow`; raises `TenantPluginDisabled` domain event
- [x] T008 Create `src/Terminar.Modules.Tenants/Application/Queries/ListPlugins/ListPluginsQuery.cs` and `ListPluginsQueryHandler.cs` — query takes `Guid TenantId`; handler fetches `IPluginCatalog.GetAll()` and joins with `ITenantPluginActivationRepository.ListForTenantAsync(...)` to produce `IReadOnlyList<PluginStatusDto>` where `PluginStatusDto` has `string Id, string Name, string Description, bool IsEnabled`
- [x] T009 Update `src/Terminar.Modules.Tenants/Infrastructure/TenantsDbContext.cs` — add `DbSet<TenantPluginActivation> TenantPluginActivations`; add `OnModelCreating` configuration: table name `tenant_plugin_activations` in schema `tenants`, unique index `(tenant_id, plugin_id)`, index on `tenant_id`, column max-length 64 for `PluginId`
- [x] T010 Add EF Core migration `AddTenantPluginActivations` by running: `dotnet ef migrations add AddTenantPluginActivations --project src/Terminar.Modules.Tenants --startup-project src/Terminar.Api --context TenantsDbContext`; then edit the generated migration file to append a data-seed SQL call: `migrationBuilder.Sql("INSERT INTO tenants.tenant_plugin_activations (id, tenant_id, plugin_id, is_enabled, enabled_at) SELECT gen_random_uuid(), t.id, 'excusals', TRUE, NOW() FROM tenants.tenants t WHERE EXISTS (SELECT 1 FROM tenants.excusal_validity_windows w WHERE w.tenant_id = t.id) ON CONFLICT (tenant_id, plugin_id) DO NOTHING;");`
- [x] T011 Create `src/Terminar.Modules.Tenants/Infrastructure/Repositories/TenantPluginActivationRepository.cs` — EF Core implementation of `ITenantPluginActivationRepository` using `TenantsDbContext`; `FindAsync` uses `FirstOrDefaultAsync` filtered by `TenantId` and `PluginId`; `ListForTenantAsync` uses `Where(x => x.TenantId == tenantId).ToListAsync()`
- [x] T012 Update `src/Terminar.Modules.Tenants/Infrastructure/TenantsModule.cs` — register `ITenantPluginActivationRepository` → `TenantPluginActivationRepository` as scoped
- [x] T013 Create `src/Terminar.Api/Plugins/PluginCatalog.cs` — `InMemoryPluginCatalog : IPluginCatalog` with a `ConcurrentDictionary<string, PluginDescriptor>`; `Register(PluginDescriptor descriptor)` method for population at startup; `GetAll()` returns all descriptors; `IsRegistered(string id)` checks the dictionary
- [x] T014 Create `src/Terminar.Api/Plugins/PluginGuardFilter.cs` — `PluginGuardFilter(string pluginId, ITenantPluginActivationRepository repo, ITenantContext tenantCtx) : IEndpointFilter`; in `InvokeAsync`: resolve `tenantId` from `tenantCtx`, call `repo.IsEnabledAsync(tenantId, pluginId, ct)`, return `Results.UnprocessableEntity(new { error = "plugin_not_enabled", plugin_id = pluginId })` if inactive, else `await next(ctx)`; create companion `PluginGuardFilterFactory` class that builds a `PluginGuardFilter` for a given plugin ID via DI

**Checkpoint**: Foundation complete — plugin catalog, guard, persistence, and commands/queries are in place. User story work can begin.

---

## Phase 3: User Story 1 — Tenant Admin Activates Excusal Plugin (Priority: P1) 🎯 MVP

**Goal**: A tenant admin can activate/deactivate the Excusal plugin from global settings. When inactive, all excusal API endpoints return 422 and excusal UI elements are hidden. When active, excusal features are fully accessible.

**Independent Test**: Activate excusal plugin via POST /api/v1/settings/plugins/excusals/activate → verify excusal endpoints succeed. Deactivate via DELETE → verify all excusal endpoints return 422. Verify excusal nav links and routes disappear in the frontend when plugin is inactive.

- [x] T015 [US1] Create `src/Terminar.Api/Modules/PluginsModule.cs` — register three routes with `RequireAuthorization`: `GET /api/v1/settings/plugins` (StaffOrAdmin) dispatches `ListPluginsQuery`; `POST /api/v1/settings/plugins/{pluginId}/activate` (AdminOnly) dispatches `EnablePluginCommand`; `DELETE /api/v1/settings/plugins/{pluginId}/activate` (AdminOnly) dispatches `DisablePluginCommand`; return `404 Not Found` if plugin ID not in catalog (catch `InvalidOperationException`), `200 OK` or `204 No Content` on success
- [x] T016 [US1] Create `src/Terminar.Api/Plugins/ExcusalPlugin.cs` — implement `ITerminarPlugin`; `Descriptor` returns `new PluginDescriptor("excusals", "Excusals", "Allows participants to be excused from courses and receive credits for future enrollment.")`; `RegisterServices` is empty (all excusal DI is already registered by existing modules); `MapEndpoints(app, guardFilter)` creates two guarded route groups: one for excusal credits routes (currently in `ExcusalCreditsModule.cs`) and one for excusal settings routes (currently in `ExcusalSettingsModule.cs`) — copy the route registrations from those modules into the two groups, wrapping each group with `.AddEndpointFilter(guardFilter)`
- [x] T017 [US1] Create `src/Terminar.Api/Plugins/PluginExtensions.cs` — extension method `AddTerminarPlugin<TPlugin>(this IServiceCollection services) where TPlugin : class, ITerminarPlugin` that registers `TPlugin` as singleton `ITerminarPlugin` and calls `plugin.RegisterServices(services)`; add extension `UseTerminarPlugins(this WebApplication app)` that resolves all `IEnumerable<ITerminarPlugin>`, registers each descriptor in `IPluginCatalog`, then calls `plugin.MapEndpoints(app, factory.CreateFor(plugin.Descriptor.Id))` for each
- [x] T018 [US1] Update `src/Terminar.Api/Program.cs` — add `builder.Services.AddTerminarPlugin<ExcusalPlugin>()`, register `IPluginCatalog` as singleton `InMemoryPluginCatalog`, register `PluginGuardFilterFactory`; call `app.UseTerminarPlugins()` after `app.UseAuthorization()`; add `app.MapPluginEndpoints()` call for `PluginsModule`
- [x] T019 [P] [US1] Create `frontend/src/shared/plugins/types.ts` — export `PluginStatus { id, name, description, isEnabled }`, `NavItem { path, labelKey, icon, requiredRole? }`, `PluginContribution { id, routes: RouteObject[], navItems: NavItem[] }`
- [x] T020 [P] [US1] Create `frontend/src/shared/plugins/pluginsApi.ts` — export `fetchPlugins(): Promise<PluginStatus[]>` (GET /api/v1/settings/plugins), `activatePlugin(pluginId: string): Promise<void>` (POST /api/v1/settings/plugins/{id}/activate), `deactivatePlugin(pluginId: string): Promise<void>` (DELETE /api/v1/settings/plugins/{id}/activate); use existing `apiClient` from `@/shared/api/client`
- [x] T021 [US1] Create `frontend/src/shared/plugins/useActivePlugins.ts` — `useActivePlugins(): string[]` hook using TanStack Query: `queryKey: ['plugins']`, `queryFn: fetchPlugins`, `staleTime: 5 * 60 * 1000`; returns array of `id` for all plugins where `isEnabled === true`; returns `[]` while loading or unauthenticated
- [x] T022 [P] [US1] Create `frontend/src/features/excusal-credits/plugin.ts` — export `excusalPluginContribution: PluginContribution` with `id: 'excusals'`; routes: `{ path: 'excusal-credits', element: <ExcusalCreditsPage /> }` and `{ path: 'settings/excusal', element: <ExcusalSettingsPage /> }`; navItems: `{ path: '/app/excusal-credits', labelKey: 'nav.excusalCredits', icon: IconCertificate }` and `{ path: '/app/settings/excusal', labelKey: 'nav.excusalSettings', icon: IconSettings, requiredRole: 'Admin' }`
- [x] T023 [US1] Update `frontend/src/app/router.tsx` — remove hardcoded `ExcusalCreditsPage` and `ExcusalSettingsPage` imports and their route entries; instead, after all static routes in the `/app` children array, spread plugin routes (import `excusalPluginContribution` from the plugin file and spread its routes conditionally — since router is static, use a provider pattern: wrap the router in a `PluginAwareRouter` component that uses `useActivePlugins` and recreates the router when active plugins change using `RouterProvider`; alternatively, add all plugin routes unconditionally and guard access via `PluginGuard` wrapper component at the route element level — use the simpler option of always including the routes but wrapping page components in a `PluginGuard` component that redirects to 404 if plugin inactive)
- [x] T024 [US1] Create `frontend/src/shared/plugins/PluginGuard.tsx` — component `PluginGuard({ pluginId, children })` that calls `useActivePlugins()` and renders `children` if `pluginId` is in the active list, otherwise renders `<NotFoundPage />` (or navigates to `/app`); use this to wrap excusal route elements in `router.tsx`
- [x] T025 [US1] Update `frontend/src/shared/components/AppShellLayout.tsx` — remove hardcoded excusal credits and excusal settings `NavLink` entries; add dynamic nav section that calls `useActivePlugins()`, then maps active plugin IDs through a static nav-item registry (inline or imported from plugin contribution files) to render nav links; filter by `requiredRole` the same way as existing staff/admin checks
- [x] T026 [P] [US1] Update `frontend/src/shared/i18n/locales/en.json` and `cs.json` — ensure `nav.excusalCredits`, `nav.excusalSettings` keys exist (they likely already do; verify and add if missing); add `plugins.title: "Plugins"`, `plugins.activate: "Activate"`, `plugins.deactivate: "Deactivate"`, `plugins.active: "Active"`, `plugins.inactive: "Inactive"`

**Checkpoint**: User Story 1 complete — excusal plugin can be toggled, backend guard is live, frontend shows/hides excusal nav and pages based on activation state.

---

## Phase 4: User Story 2 — Excusal Behavior Unchanged (Priority: P2)

**Goal**: All existing excusal workflows (create excusal, issue credit, redeem credit, emails) work identically when the plugin is active. Existing tenants that use excusals are automatically activated after migration.

**Independent Test**: With excusal plugin active, run through all three acceptance scenarios from spec: (1) excusal created → credit issued + email sent, (2) credit redeemed + email sent, (3) existing tenants have plugin auto-activated after migration.

- [x] T027 [US2] Remove old excusal endpoint module calls from `src/Terminar.Api/Program.cs` — delete the lines that call `app.MapExcusalCreditsEndpoints()` and `app.MapExcusalSettingsEndpoints()` (these are now registered by `ExcusalPlugin.MapEndpoints` via `UseTerminarPlugins()`); verify the app still compiles and existing test suite passes
- [x] T028 [US2] Update `frontend/src/features/courses/components/CourseExcusalPolicySection.tsx` — add `useActivePlugins()` call at the top of the component; if `'excusals'` is not in the active plugins list, return `null` (render nothing); this hides the excusal policy panel on the course detail page when the plugin is inactive
- [x] T029 [US2] Run migrations against the local dev database: `dotnet ef database update --project src/Terminar.Modules.Tenants --startup-project src/Terminar.Api --context TenantsDbContext`; verify the `tenant_plugin_activations` table exists and that any tenant with existing excusal validity windows has an `is_enabled = true` row for `plugin_id = 'excusals'`
- [x] T030 [US2] Manual smoke test — with excusal plugin active for a tenant: (1) create an excusal via POST /api/v1/excusals → confirm 200 + excusal credit created; (2) redeem a credit → confirm 200 + email notification triggered; (3) with plugin deactivated → confirm same endpoint returns 422 with `{ error: "plugin_not_enabled" }` body

**Checkpoint**: User Story 2 complete — existing excusal functionality is behaviorally identical; migration auto-activates existing tenants; endpoint guard is live.

---

## Phase 5: User Story 3 — Tenant Admin Views and Manages Plugin Catalog (Priority: P2)

**Goal**: Tenant admin sees a plugin management page in Settings listing all available plugins with their status and can toggle activation from the UI.

**Independent Test**: Log in as Admin, navigate to Settings → Plugins; both Excusals (active) and Payments (inactive stub) are listed with names, descriptions, and toggle buttons. Activating/deactivating updates state immediately.

- [x] T031 [US3] Create `frontend/src/features/plugins/PluginsSettingsPage.tsx` — fetch plugin list with `useQuery({ queryKey: ['plugins'], queryFn: fetchPlugins })`; render a Mantine `Table` or `Stack` of `Card` components per plugin showing name, description, and a `Switch` or `Button` for activate/deactivate; on toggle call `activatePlugin`/`deactivatePlugin` and invalidate the `['plugins']` query to refresh; show loading state while fetching; only render if user is Admin (guard with `session?.role === 'Admin'`)
- [x] T032 [US3] Update `frontend/src/app/router.tsx` — add static route `{ path: 'settings/plugins', element: <PluginsSettingsPage /> }` inside the `/app` children (this route does not go through `PluginGuard` since the settings page itself should always be visible to admins)
- [x] T033 [US3] Update `frontend/src/shared/components/AppShellLayout.tsx` — add a static admin-only `NavLink` for `/app/settings/plugins` with label `t('nav.pluginsSettings')` and an appropriate icon (e.g., `IconPuzzle` from `@tabler/icons-react`)
- [x] T034 [P] [US3] Update `frontend/src/shared/i18n/locales/en.json` — add `nav.pluginsSettings: "Plugins"`, plugin description strings `plugins.excusals.description: "Allow participants to be excused from courses and receive credits for future enrollment."`, `plugins.payments.description: "Collect payment for course registrations."`; update `frontend/src/shared/i18n/locales/cs.json` with Czech translations for the same keys

**Checkpoint**: User Story 3 complete — admin can view all plugins and toggle activation from the Settings → Plugins page.

---

## Phase 6: User Story 4 — New Plugin Without Core System Changes (Priority: P3)

**Goal**: A stub Payments plugin is registered using the plugin contract without modifying any core code. It appears in the plugin catalog and its activation state is respected by the system.

**Independent Test**: After completing this phase, the Payments plugin appears in GET /api/v1/settings/plugins and in the frontend Settings → Plugins page. Activating it does not break anything; deactivating it returns its guarded endpoints as 422 (stub has no real endpoints, but the pattern is demonstrably correct).

- [x] T035 [P] [US4] Create `src/Terminar.Api/Plugins/PaymentsPlugin.cs` — implement `ITerminarPlugin`; `Descriptor` returns `new PluginDescriptor("payments", "Payments", "Collect payment for course registrations.")`; `RegisterServices` is empty; `MapEndpoints` registers a single stub route `GET /api/v1/payments/status` inside a guarded group returning `Results.Ok(new { status = "payments_stub" })` — this proves the guard pattern works for a new plugin
- [x] T036 [US4] Update `src/Terminar.Api/Program.cs` — add `builder.Services.AddTerminarPlugin<PaymentsPlugin>()` (the plugin catalog and guard wiring are handled automatically by `UseTerminarPlugins()`)
- [x] T037 [P] [US4] Create `frontend/src/features/payments/plugin.ts` — export `paymentsPluginContribution: PluginContribution` with `id: 'payments'`, empty `routes: []`, and `navItems: []` (stub has no real UI pages); this is sufficient to prove the frontend registry accepts a new plugin entry
- [x] T038 [US4] Create `frontend/src/shared/plugins/pluginRegistry.ts` — export a `PLUGIN_REGISTRY: Record<string, PluginContribution>` map containing both `excusals: excusalPluginContribution` and `payments: paymentsPluginContribution`; export `getContributionsForActivePlugins(activeIds: string[]): PluginContribution[]` that maps IDs through the registry (skipping unknown IDs); update `AppShellLayout` and `router.tsx` to use this registry function instead of directly importing individual plugin contributions

**Checkpoint**: User Story 4 complete — Payments stub appears in the catalog; adding a third plugin would require only creating a plugin class + plugin.ts file + registry entry.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [x] T039 [P] Delete `src/Terminar.Api/Modules/ExcusalCreditsModule.cs` and `src/Terminar.Api/Modules/ExcusalSettingsModule.cs` if they are now empty shells with no remaining content after the endpoint registrations were moved into `ExcusalPlugin.cs` in T016; verify no remaining references to the deleted classes elsewhere in the codebase
- [x] T040 Run the quickstart.md end-to-end checklist manually: add a hypothetical third plugin (even just on a scratch branch) following the 7-step guide and confirm all checklist items pass; update `specs/007-plugin-architecture/quickstart.md` if any steps are incorrect or out of date

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS all user stories**
- **Phase 3 (US1, P1)**: Depends on Phase 2 — can start as soon as foundational is done
- **Phase 4 (US2, P2)**: Depends on Phase 3 being complete (ExcusalPlugin.MapEndpoints must exist before removing old module wiring)
- **Phase 5 (US3, P2)**: Depends on Phase 3 (needs useActivePlugins + pluginsApi); can run in parallel with Phase 4
- **Phase 6 (US4, P3)**: Depends on Phase 2 + Phase 3; can run in parallel with Phases 4 and 5
- **Phase 7 (Polish)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: After Foundational only — no story-to-story dependencies
- **US2 (P2)**: After US1 (needs ExcusalPlugin to exist before removing old module wiring)
- **US3 (P2)**: After US1 (needs useActivePlugins, pluginsApi, pluginRegistry); parallel with US2
- **US4 (P3)**: After Foundational; parallel with US1/US2/US3 for the stub work

### Within Each Phase

- Domain tasks before Application tasks
- Application tasks before Infrastructure tasks
- Infrastructure tasks before API tasks
- Backend tasks before corresponding frontend tasks (API contract must exist for frontend to call)
- All [P]-marked tasks within a phase can run in parallel

---

## Parallel Execution Examples

### Phase 2: Foundational — Maximum Parallelism

```
Parallel batch 1 (Domain):
  T002: TenantPluginActivation entity
  T003: TenantPluginEnabled event
  T004: TenantPluginDisabled event

Sequential: T005 (repository interface) → T006 + T007 (application commands, parallel) → T008 → T009 → T010 + T011 (parallel) → T012 + T013 + T014 (parallel)
```

### Phase 3: US1 — Frontend Tasks Can Run in Parallel with Backend

```
Backend sequence:
  T015 → T016 → T017 → T018

Frontend (can start after T015 API contract is known):
  T019 [P]: types.ts
  T020 [P]: pluginsApi.ts
  (then) T021 → T022 [P] + T024 [P] → T023 → T025 → T026 [P]
```

### Phase 5/6 — Can Run Concurrently

```
Developer A: Phase 5 (US3) — PluginsSettingsPage + nav link + i18n
Developer B: Phase 6 (US4) — PaymentsPlugin backend + frontend stub
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T014) — **critical blocker**
3. Complete Phase 3: US1 (T015–T026)
4. **STOP and VALIDATE**: Backend guard live, frontend shows/hides excusal features
5. Ship: all existing excusal tenants continue working; new tenants can choose to activate

### Incremental Delivery

1. Phase 1 + Phase 2 → Plugin infrastructure deployed (invisible to end users)
2. Phase 3 → US1: Plugin toggle mechanism live (admin can activate/deactivate)
3. Phase 4 → US2: Migration seeded, excusal rewiring verified (existing tenants unaffected)
4. Phase 5 → US3: Plugin settings page visible in admin UI
5. Phase 6 → US4: Payments stub proves extensibility (developer confidence)
6. Phase 7 → Polish: Cleanup and quickstart validation

---

## Notes

- [P] tasks = different files, no shared dependencies within the phase
- DDD dependency direction within foundational phase: Domain → Application → Infrastructure → API
- Do not start US2 wiring (T027) until ExcusalPlugin.MapEndpoints is complete (T016)
- The data migration seed in T010 is embedded in the EF Core migration — it runs automatically with `dotnet ef database update`
- `CourseExcusalPolicySection.tsx` guard (T028) is the only frontend file in US2; all other US2 tasks are backend/verification
- Tasks T039–T040 (Polish) can be done in any order and do not block each other
