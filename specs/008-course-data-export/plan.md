# Implementation Plan: Course Data Export

**Branch**: `008-course-data-export` | **Date**: 2026-04-04 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/008-course-data-export/spec.md`

## Summary

Add CSV export of courses and their participants as an integral part of the application. Staff members can export the courses list (optionally including all participant enrollment data) from the courses list page, and export a single course's participant roster from the course detail/roster page. Exports are configurable with date range filters, status filters, and per-column toggles. No new database tables are required — this is a pure read feature. CSV generation uses CsvHelper (new dependency) with UTF-8 BOM for correct Excel rendering. The API layer orchestrates data from the Courses and Registrations modules via MediatR and returns a file download response.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MediatR 12.x, CsvHelper (new), Mantine v9, TanStack Query v5, react-i18next  
**Storage**: PostgreSQL via EF Core 10 — no new tables or migrations  
**Testing**: xUnit (backend unit/integration), Vitest (frontend)  
**Target Platform**: Web (server-rendered API + SPA)  
**Project Type**: Web service + SPA  
**Performance Goals**: Export of 10 000 rows completes within 30 seconds  
**Constraints**: RFC 4180 CSV compliance; UTF-8 BOM required; tenant isolation enforced on every query  
**Scale/Scope**: Per-tenant exports; typical size 100–500 rows; maximum supported 10 000 rows (synchronous)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Principle I — Domain-Driven Design ✅

Export is a read-only application-layer concern. No business logic is introduced. New query handlers (`ExportCoursesQuery`, `ExportCourseRosterQuery`) live in the Application layer of their respective modules, delegating to existing repositories. The domain layer is not modified. CSV serialization lives in the Infrastructure/API layer (`CsvExportService`), not the domain.

### Principle II — Multi-Tenancy by Default ✅

All export queries filter by `TenantId` explicitly (same pattern as existing handlers). `TenantId` is extracted from `ITenantContext` at the API endpoint level and passed into every query. No cross-tenant data access is possible.

### Principle III — Multi-Language First ✅

All new UI strings (modal titles, button labels, column names) are added to both `en` and `cs` i18n JSON files. CSV column headers are generated from i18n keys using the tenant's configured language. No hardcoded user-facing strings.

### Principle IV — Clean Architecture ✅

Dependency direction is preserved:
- Domain layer: untouched
- Application layer: new query handlers in Courses and Registrations modules only
- Infrastructure/API layer: `CsvExportService` (CSV generation), new endpoints in `CoursesModule.cs`
- `CsvExportService` lives in `Terminar.Api` (presentation layer) — it receives DTOs from application layer, not domain objects

### Principle V — Spec-First Development ✅

`spec.md` was authored and validated before this plan. No code has been written ahead of spec approval.

### Complexity Tracking

No constitution violations. No complexity justification required.

## Project Structure

### Documentation (this feature)

```text
specs/008-course-data-export/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   └── api-export.md    ← Phase 1 output
├── checklists/
│   └── requirements.md  ← Spec quality checklist
└── tasks.md             ← Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code

```text
src/
├── Terminar.Modules.Courses/
│   └── Application/
│       └── Queries/
│           └── ExportCourses/
│               ├── ExportCoursesQuery.cs          ← NEW
│               ├── ExportCoursesHandler.cs         ← NEW
│               └── ExportCourseRowDto.cs           ← NEW
│
├── Terminar.Modules.Registrations/
│   └── Application/
│       └── Queries/
│           └── ExportCourseRoster/
│               ├── ExportCourseRosterQuery.cs      ← NEW
│               ├── ExportCourseRosterHandler.cs    ← NEW
│               └── ExportParticipantRowDto.cs      ← NEW
│
└── Terminar.Api/
    ├── Modules/
    │   └── CoursesModule.cs                        ← MODIFIED (add 3 endpoints)
    └── Services/
        ├── CsvExportService.cs                     ← NEW
        └── ExportColumnDefinition.cs               ← NEW

frontend/src/
├── features/
│   ├── courses/
│   │   ├── CourseListPage.tsx                      ← MODIFIED (add Export button)
│   │   ├── ExportCoursesModal.tsx                  ← NEW
│   │   └── coursesApi.ts                           ← MODIFIED (add getExportColumns)
│   └── registrations/
│       ├── CourseRosterPage.tsx                    ← MODIFIED (add Export CSV button)
│       ├── ExportRosterModal.tsx                   ← NEW
│       └── registrationsApi.ts                     ← MODIFIED (add export column support)
├── lib/
│   └── download.ts                                 ← NEW
└── i18n/
    ├── en.json                                     ← MODIFIED (add export keys)
    └── cs.json                                     ← MODIFIED (add export keys)
```

**Structure Decision**: Follows the established modular DDD structure. New application-layer queries are added to their owning module. CSV generation and column definitions live in `Terminar.Api` (presentation layer) since they are output-format concerns, not domain or application concerns. The frontend adds components co-located with their feature areas.

---

## Phase 0: Research

**Status**: Complete — see [research.md](research.md)

Key decisions from research:
- **CsvHelper** NuGet package for RFC 4180-compliant CSV with UTF-8 BOM
- **API-layer orchestration** for cross-module data assembly (no shared DbContexts)
- **Two new non-paginated query handlers** per module for export
- **`Results.File()`** for file download response in Minimal APIs
- **`download.ts` utility** in frontend for binary fetch + browser download trigger
- **SessionStorage** for persisting last-used column/filter settings within browser session
- **`GET /api/v1/courses/export/columns`** endpoint to serve dynamic column definitions (including custom fields) to frontend

---

## Phase 1: Design & Contracts

**Status**: Complete

### Artifacts Generated

- [data-model.md](data-model.md) — Export DTOs, column groups, no new DB tables
- [contracts/api-export.md](contracts/api-export.md) — Full API contract for 3 new endpoints
- [quickstart.md](quickstart.md) — Developer walkthrough for testing the feature

### Key Design Decisions

#### Backend

1. **`ExportCoursesQuery`** (Courses module Application layer):
   - Parameters: `TenantId`, `DateFrom?`, `DateTo?`, `Status?`
   - Returns: `List<ExportCourseRowDto>` — all matching courses with session data (no pagination)
   - Uses existing `CourseRepository` pattern; adds `Include(c => c.Sessions)` for location/dates
   - Does **not** include registration counts — those come from the Registrations module

2. **`ExportCourseRosterQuery`** (Registrations module Application layer):
   - Parameters: `TenantId`, `CourseIds` (list for bulk fetch), `IncludeExcusalCounts` (bool)
   - Returns: `List<ExportParticipantRowDto>` — all registrations with field values (no pagination)
   - Fetches all `ParticipantFieldValue` records via `Include`; maps to column keyed by `FieldDefinitionId`
   - Calls `ListCustomFieldDefinitionsQuery` cross-module (existing pattern) to get field names
   - Optionally calls `GetExcusalCountsForCourseQuery` if `IncludeExcusalCounts = true`

3. **`CsvExportService`** (API layer):
   - Receives column selection + DTOs from both queries
   - Uses CsvHelper `CsvWriter` with dynamic column configuration
   - Returns `byte[]` (UTF-8 BOM encoded)
   - Separate methods: `BuildCoursesOnlyCsv()` and `BuildWithParticipantsCsv()`

4. **Three new endpoints** in `CoursesModule.cs`:
   - `GET /api/v1/courses/export/columns` → returns `ExportColumnDefinition[]`
   - `GET /api/v1/courses/export` → calls export queries + CsvExportService, returns `Results.File`
   - `GET /api/v1/courses/{courseId}/registrations/export` → single-course roster export

#### Frontend

1. **`download.ts`** utility:
   ```typescript
   export async function downloadFile(url: string, filename: string): Promise<void>
   ```
   Uses raw `fetch()` with auth header from auth store, converts to `Blob`, triggers anchor click.

2. **`ExportCoursesModal`** component:
   - Fetches column definitions from `/api/v1/courses/export/columns` on mount
   - Groups columns into sections (Course Info, Participant Info, Custom Fields)
   - Shows participant columns only when "Include participants" is toggled on
   - Persists selections to `sessionStorage` on close/download
   - Calls `downloadFile()` with constructed query string

3. **`ExportRosterModal`** component (simplified):
   - Shows only participant + custom field columns (no course filters)
   - Downloads from `/api/v1/courses/{courseId}/registrations/export`

#### No New Migrations

The feature is read-only. Zero schema changes. No EF Core migrations to add.

---

## Constitution Check (Post-Design)

Re-evaluated after Phase 1 design. All gates remain green:

- ✅ **DDD**: All new logic is in Application layer queries. Domain untouched.
- ✅ **Multi-tenancy**: Every new query filters by `TenantId`. Verified in contracts.
- ✅ **Multi-language**: Column headers use i18n keys; CSV headers generated from tenant language.
- ✅ **Clean Architecture**: CsvExportService in API (presentation) layer; depends on Application DTOs only.
- ✅ **Spec-First**: No deviations from spec.md detected in design.
