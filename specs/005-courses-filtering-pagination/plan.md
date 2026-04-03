# Implementation Plan: Courses Filtering, Sorting & Temporal Views

**Branch**: `005-courses-filtering-pagination` | **Date**: 2026-04-03 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/005-courses-filtering-pagination/spec.md`

## Summary

Add temporal view tabs (Upcoming / Ongoing / Past), text search, status and type filters, tag filter, sortable columns, and pagination to the Courses list page. All filtering and sorting run client-side on the full course list. The backend change is minimal: add `lastSessionEndsAt` and `tags` to the `CourseListItem` DTO projection.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MediatR 12.x (backend); Mantine v9, TanStack Query v5, react-i18next (frontend)  
**Storage**: PostgreSQL via EF Core 10 — no new tables or migrations required  
**Testing**: `dotnet test` (backend); `npm run typecheck` (frontend type check)  
**Target Platform**: Web (staff portal, authenticated)  
**Project Type**: Web application (backend API + React SPA frontend)  
**Performance Goals**: Filter results visible within 1 second of user input  
**Constraints**: Client-side filtering only (no backend query parameter changes); no URL state persistence in v1  
**Scale/Scope**: <200 courses per tenant at current scale; client-side approach sufficient

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | No business logic added to frontend or application layer; DTO extension only. The temporal classification algorithm lives in a dedicated frontend hook (presentation concern), not in the domain. |
| II. Multi-Tenancy by Default | PASS | No new queries introduced. Existing `ListByTenantAsync` with tenant scoping is unchanged. |
| III. Multi-Language First | PASS | All new UI strings added to both `en.json` and `cs.json` i18n locale files. |
| IV. Clean Architecture | PASS | Backend change is an additive DTO projection in the application layer handler. No domain objects are modified. Infrastructure layer unchanged. |
| V. Spec-First Development | PASS | spec.md approved before this plan was authored. |

**Result**: All gates pass. No violations. Proceed to implementation.

## Project Structure

### Documentation (this feature)

```text
specs/005-courses-filtering-pagination/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   └── list-courses-api.md
└── tasks.md             ← Phase 2 output (/speckit.tasks command)
```

### Source Code Changes

```text
# Backend (Application layer only — no domain or infrastructure changes)
src/Terminar.Modules.Courses/
└── Application/
    └── Queries/
        └── ListCourses/
            ├── ListCoursesQuery.cs     ← Add LastSessionEndsAt + Tags to CourseListItem
            └── ListCoursesHandler.cs   ← Populate new fields from domain entity

# Frontend
frontend/src/
├── features/courses/
│   ├── types.ts                         ← Add lastSessionEndsAt + tags fields
│   ├── CourseListPage.tsx               ← Add filter UI, sortable headers, pagination
│   └── hooks/
│       └── useCoursesFilter.ts          ← NEW: filter state + derived list logic
└── shared/
    └── i18n/
        └── locales/
            ├── en.json                  ← Add new i18n keys
            └── cs.json                  ← Add new i18n keys
```

## Implementation Phases

### Phase A — Backend DTO Extension (Application Layer)

**Files**: `ListCoursesQuery.cs`, `ListCoursesHandler.cs`

1. Extend `CourseListItem` record with two new fields:
   - `DateTime? LastSessionEndsAt`
   - `List<string> Tags`

2. Update `ListCoursesHandler` projection:
   - `LastSessionEndsAt` = `c.Sessions.MaxBy(s => s.ScheduledAt)?.EndsAt`
   - `Tags` = `c.ExcusalPolicy.Tags` (owned entity, already loaded)

3. Verify build passes with `dotnet build`.

**No migrations required.** The new fields are computed from already-persisted data.

---

### Phase B — Frontend Type Update

**File**: `frontend/src/features/courses/types.ts`

Add to `CourseListItem` interface:
```typescript
lastSessionEndsAt: string | null;
tags: string[];
```

Add new derived types:
```typescript
type TemporalBucket = 'all' | 'upcoming' | 'ongoing' | 'past';
type SortField = 'title' | 'firstSessionAt' | 'capacity';
type SortDirection = 'asc' | 'desc';
```

---

### Phase C — Filter Hook

**File**: `frontend/src/features/courses/hooks/useCoursesFilter.ts` (new file)

Implement `useCoursesFilter(courses: CourseListItem[])` returning:
- Filter state: `temporalBucket`, `search`, `statuses`, `courseType`, `tags`, `sortField`, `sortDirection`
- Setters for each filter
- `filteredCourses`: derived sorted+filtered list
- `availableTags`: unique tags from all courses (for tag filter options)
- `hasActiveFilters`: boolean for "Clear all" visibility
- `clearAll()`: resets all filters to defaults
- Temporal classification logic per decision table in `research.md`
- Default sort: `firstSessionAt` ascending, nulls last

---

### Phase D — CourseListPage UI Update

**File**: `frontend/src/features/courses/CourseListPage.tsx`

1. **Temporal tabs**: Replace or add above the table — Mantine `Tabs` or `SegmentedControl` with values `all | upcoming | ongoing | past`. Connected to `useCoursesFilter`.

2. **Filter bar** (below temporal tabs, above table):
   - `TextInput` for search (debounced or instant)
   - `MultiSelect` for status filter (options: Draft, Active, Cancelled, Completed)
   - `Select` for course type (OneTime / MultiSession / All)
   - `MultiSelect` for tags — hidden when `availableTags.length === 0`
   - "Clear all" `Button` (visible only when `hasActiveFilters`)

3. **Sortable column headers**: Wrap `Table.Th` content with a clickable element that toggles sort. Show `↑` / `↓` icon next to active sort column.

4. **Pagination**: Use existing `usePagination` hook with `pageSize = 25`. Apply page slice to `filteredCourses`. Render Mantine `Pagination` component below the table. Call `pagination.reset()` inside the filter hook's setter callbacks (or watch `filteredCourses` length changes).

5. **Empty state**: When `filteredCourses.length === 0` and filters are active, show message with "Clear filters" button.

---

### Phase E — i18n Strings

**Files**: `frontend/src/shared/i18n/locales/en.json`, `cs.json`

Add keys under `courses` namespace:
- `courses.temporal.all`, `courses.temporal.upcoming`, `courses.temporal.ongoing`, `courses.temporal.past`
- `courses.filters.search`, `courses.filters.status`, `courses.filters.type`, `courses.filters.tags`, `courses.filters.clearAll`
- `courses.noResults`, `courses.noResultsHint` (empty state under filters)
- `courses.sort.asc`, `courses.sort.desc`

## Complexity Tracking

No constitution violations — this section is not required.
