# Tasks: Courses Filtering, Sorting & Temporal Views

**Input**: Design documents from `/specs/005-courses-filtering-pagination/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not requested in spec — no test tasks generated.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US5)

---

## Phase 1: Setup

No new project initialization required — implementation targets files within the existing project structure. The feature branch `005-courses-filtering-pagination` is already checked out.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Backend DTO extension and frontend type/hook foundation that ALL user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Extend `CourseListItem` record in `src/Terminar.Modules.Courses/Application/Queries/ListCourses/ListCoursesQuery.cs` — add `DateTime? LastSessionEndsAt` and `List<string> Tags` parameters to the record
- [x] T002 Update `ListCoursesHandler.cs` in `src/Terminar.Modules.Courses/Application/Queries/ListCourses/ListCoursesHandler.cs` — populate `LastSessionEndsAt` as `c.Sessions.MaxBy(s => s.ScheduledAt)?.EndsAt` and `Tags` as `c.ExcusalPolicy.Tags` in the `Select` projection; verify `dotnet build` passes
- [x] T003 [P] Extend `CourseListItem` interface in `frontend/src/features/courses/types.ts` — add `lastSessionEndsAt: string | null` and `tags: string[]` fields; add new union types `type TemporalBucket = 'all' | 'upcoming' | 'ongoing' | 'past'`, `type SortField = 'title' | 'firstSessionAt' | 'capacity'`, `type SortDirection = 'asc' | 'desc'`; add `CourseFilters` interface with fields: `temporalBucket`, `search`, `statuses`, `courseType`, `tags`, `sortField`, `sortDirection`
- [x] T004 [P] Create `frontend/src/features/courses/hooks/useCoursesFilter.ts` — skeleton hook that accepts `courses: CourseListItem[]`, initialises all `CourseFilters` fields to defaults (`temporalBucket: 'all'`, `search: ''`, `statuses: []`, `courseType: null`, `tags: []`, `sortField: 'firstSessionAt'`, `sortDirection: 'asc'`), and returns the filter state, setters, a stub `filteredCourses` (the full list for now), `availableTags: string[]` (empty for now), `hasActiveFilters: false` (for now), and a stub `clearAll()` no-op

**Checkpoint**: Backend compiles (`dotnet build`); frontend compiles (`npm run typecheck`) with the new types and hook skeleton.

---

## Phase 3: User Story 1 — Temporal Classification (Priority: P1) 🎯 MVP

**Goal**: Staff can filter the course list by Upcoming / Ongoing / Past / All using temporal tabs.

**Independent Test**: Navigate to Courses → four tabs appear (All, Upcoming, Ongoing, Past); clicking each shows only the courses matching that temporal window; courses with no sessions appear under Upcoming only.

### Implementation

- [x] T005 [US1] Implement temporal bucket classification logic in `frontend/src/features/courses/hooks/useCoursesFilter.ts` — add a `classifyBucket(course: CourseListItem, now: Date): TemporalBucket` helper function using the algorithm: `upcoming` when `firstSessionAt` is null OR `firstSessionAt > now`; `past` when status is `Cancelled` or `Completed`, OR `lastSessionEndsAt` is non-null and `<= now`; `ongoing` otherwise (started but not yet ended); update `filteredCourses` derivation to apply `temporalBucket` filter using this helper
- [x] T006 [US1] Add temporal tab UI to `frontend/src/features/courses/CourseListPage.tsx` — add a Mantine `SegmentedControl` (or `Tabs`) above the table with values `all | upcoming | ongoing | past`; connect its value/onChange to `temporalBucket` setter from `useCoursesFilter`; use i18n keys `courses.temporal.all`, `courses.temporal.upcoming`, `courses.temporal.ongoing`, `courses.temporal.past`
- [x] T007 [P] [US1] Add temporal i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` — add under `courses.temporal`: `all`, `upcoming`, `ongoing`, `past` in both English and Czech

**Checkpoint**: Temporal tabs are fully functional and independently testable. Staff can distinguish Upcoming / Ongoing / Past courses without using any other filter.

---

## Phase 4: User Story 2 — Text Search and Status Filtering (Priority: P2)

**Goal**: Staff can search courses by name and filter by status (Draft / Active / Cancelled / Completed).

**Independent Test**: Type a partial course title → list filters in real time; select "Draft" in status filter → only Draft courses remain; both filters work simultaneously; "Clear all" button appears and restores the full list.

### Implementation

- [x] T008 [US2] Add search and status filter logic to `frontend/src/features/courses/hooks/useCoursesFilter.ts` — extend `filteredCourses` derivation to apply: (1) case-insensitive partial title match when `search` is non-empty; (2) status inclusion filter when `statuses` array is non-empty; chain these with the existing temporal filter
- [x] T009 [US2] Implement `hasActiveFilters` boolean and `clearAll()` in `frontend/src/features/courses/hooks/useCoursesFilter.ts` — `hasActiveFilters` is `true` when any filter differs from its default; `clearAll()` resets all filter state to defaults
- [x] T010 [US2] Add filter bar to `frontend/src/features/courses/CourseListPage.tsx` — below the temporal tabs and above the table, add: (1) a Mantine `TextInput` for search (controlled, connected to `search` setter); (2) a Mantine `MultiSelect` for status filter with options Draft/Active/Cancelled/Completed (localised); (3) a "Clear all" `Button` that calls `clearAll()`, visible only when `hasActiveFilters` is true; use i18n keys `courses.filters.search`, `courses.filters.status`, `courses.filters.clearAll`
- [x] T011 [P] [US2] Add search/status/clearAll i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` — add under `courses.filters`: `search` (placeholder text), `status` (label), `clearAll` in both languages

**Checkpoint**: Text search and status filter work independently and in combination. "Clear all" restores full list. Temporal tabs from Phase 3 remain unaffected.

---

## Phase 5: User Story 3 — Sortable Columns (Priority: P3)

**Goal**: Staff can click column headers to sort by Title, First Session date, or Capacity.

**Independent Test**: Click the "First Session" header → courses sort ascending by date with a ↑ indicator; click again → descending sort with ↓ indicator; active sort is preserved when switching temporal tabs.

### Implementation

- [x] T012 [US3] Add sort state and sort logic to `frontend/src/features/courses/hooks/useCoursesFilter.ts` — extend `filteredCourses` derivation to apply sort after filtering: sort by `title` (locale-aware string), `firstSessionAt` (date, nulls last when ascending), or `capacity` (numeric); respect `sortDirection`; add `toggleSort(field: SortField)` function that sets the field and toggles direction (or defaults to `asc` when switching fields)
- [x] T013 [US3] Make Title, First Session, and Capacity column headers sortable in `frontend/src/features/courses/CourseListPage.tsx` — wrap each `Table.Th` content in a clickable element (button or ActionIcon) that calls `toggleSort` with the corresponding field; display a ↑ or ↓ icon next to the active sort column using the current `sortDirection`; non-sortable headers (Type, Status, Sessions) remain plain text
- [x] T014 [P] [US3] Add sort i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` — add `courses.sort.asc` and `courses.sort.desc` (accessible labels for sort direction icons)

**Checkpoint**: All three sortable columns toggle correctly, direction indicator is visible, and sort persists across temporal tab changes.

---

## Phase 6: User Story 4 — Pagination (Priority: P4)

**Goal**: The course list is paginated at 25 items per page; pagination resets on any filter/sort change.

**Independent Test**: With more than 25 courses matching current filters, pagination controls appear at the bottom; clicking page 2 shows the next 25 courses; applying any filter resets to page 1.

### Implementation

- [x] T015 [US4] Integrate `usePagination` into `frontend/src/features/courses/CourseListPage.tsx` — call `usePagination(25)` at the top of the component; apply the page slice to `filteredCourses` before rendering: `pagedCourses = filteredCourses.slice((page - 1) * pageSize, page * pageSize)`; pass `pagedCourses` to the table render
- [x] T016 [US4] Add Mantine `Pagination` component below the course table in `frontend/src/features/courses/CourseListPage.tsx` — render only when `filteredCourses.length > pageSize`; bind `total={Math.ceil(filteredCourses.length / pageSize)}` and `value={page}` and `onChange={setPage}`
- [x] T017 [US4] Wire pagination reset to filter and sort changes in `frontend/src/features/courses/CourseListPage.tsx` — call `pagination.reset()` (or `setPage(1)`) inside each filter setter callback, or use a `useEffect` that watches the filter state and calls `reset()` whenever any filter/sort value changes

**Checkpoint**: Pagination controls appear correctly, pages advance properly, and any filter or sort change snaps back to page 1.

---

## Phase 7: User Story 5 — Course Type and Tag Filtering (Priority: P5)

**Goal**: Staff can filter by course type (One-Time / Multi-Session) and by tags (when tags exist on any course).

**Independent Test**: Select "Multi-Session" in the type filter → only multi-session courses appear; if any course has tags, a tag filter appears; selecting a tag narrows to matching courses; tag filter is hidden when no courses have tags.

### Implementation

- [x] T018 [US5] Add course type and tag filter logic to `frontend/src/features/courses/hooks/useCoursesFilter.ts` — extend `filteredCourses` derivation to apply: (1) course type filter when `courseType` is non-null; (2) tag inclusion filter when `tags` array is non-empty (course must contain at least one selected tag); add `availableTags` derivation as a sorted unique list of all tags across all (unfiltered) courses
- [x] T019 [US5] Add course type Select to the filter bar in `frontend/src/features/courses/CourseListPage.tsx` — use a Mantine `Select` with options: null/"All types", "OneTime", "MultiSession" (localised); connect to `courseType` setter; use i18n key `courses.filters.type`
- [x] T020 [US5] Add tag MultiSelect to the filter bar in `frontend/src/features/courses/CourseListPage.tsx` — render a Mantine `MultiSelect` populated with `availableTags`; connect to `tags` setter; only render this component when `availableTags.length > 0`; use i18n key `courses.filters.tags`
- [x] T021 [P] [US5] Add type/tag i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` — add under `courses.filters`: `type` (label + "all types" placeholder), `tags` (label) in both languages

**Checkpoint**: Type filter narrows the list correctly; tag filter appears only when tags exist and filters by tag; both combine correctly with temporal, search, and status filters.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Empty state, edge cases, and final validation.

- [x] T022 [P] Add empty state UI to `frontend/src/features/courses/CourseListPage.tsx` — when `filteredCourses.length === 0` and `hasActiveFilters` is true, show a `Center` with a message (i18n key `courses.noResults`) and a "Clear filters" Button that calls `clearAll()`; distinct from the existing "no courses at all" empty state (which shows when `courses.length === 0`)
- [x] T023 [P] Add `noResults` i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` — add `courses.noResults` (e.g., "No courses match your filters") and `courses.noResultsHint` (e.g., "Try adjusting or clearing your filters") in both languages
- [x] T024 Run `npm run typecheck` in `frontend/` and fix any TypeScript errors introduced by the new fields, types, or hook
- [x] T025 Manual validation per `specs/005-courses-filtering-pagination/quickstart.md` — verify all 5 user story independent tests pass; confirm temporal classification is accurate for Upcoming / Ongoing / Past; confirm pagination resets on filter change; confirm tag filter hides when no tags exist

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — start immediately
- **US1 (Phase 3)**: Depends on Foundational (T001–T004)
- **US2 (Phase 4)**: Depends on Foundational (T001–T004); can start in parallel with US1
- **US3 (Phase 5)**: Depends on Foundational (T001–T004); can start in parallel with US1/US2
- **US4 (Phase 6)**: Depends on Foundational (T001–T004); can start in parallel with US1–US3
- **US5 (Phase 7)**: Depends on Foundational (T001–T004) and T018 requires `availableTags` concept from Phase 2 hook skeleton; can start in parallel with other stories
- **Polish (Phase 8)**: Depends on all US phases complete (T009 `hasActiveFilters` must exist for T022)

### Within Each User Story

- Hook logic tasks (useCoursesFilter updates) BEFORE page UI tasks (CourseListPage updates)
- i18n tasks [P] can run in parallel with hook or UI tasks (different files)
- US2: T009 (`hasActiveFilters`) must complete before T010 ("Clear all" button)

### Parallel Opportunities

- **Foundational phase**: T003 and T004 are independent of each other [P]; T001 and T002 must run sequentially (T002 depends on T001)
- **T001+T002** (backend) can run in parallel with **T003+T004** (frontend) if backend and frontend are developed by different people
- **i18n tasks** (T007, T011, T014, T021, T023) can always run in parallel with implementation tasks in the same phase
- **Phase 3 (US1)** can overlap with Phase 4 (US2) once Foundational is done — they touch the same hook file but add distinct filter dimensions

---

## Parallel Example: Foundational Phase

```
# Can run in parallel (different files):
T003 — frontend/src/features/courses/types.ts
T004 — frontend/src/features/courses/hooks/useCoursesFilter.ts

# Must be sequential:
T001 → T002 — same backend file pair (query → handler)
```

## Parallel Example: User Story 1

```
# Sequential (T005 modifies hook, T006 consumes hook):
T005 → T006

# Can run in parallel with T005 or T006 (different files):
T007 — en.json + cs.json
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (T001–T004)
2. Complete Phase 3: User Story 1 (T005–T007)
3. **STOP and VALIDATE**: Navigate to Courses — temporal tabs appear and classify courses correctly
4. Deploy/demo if ready

### Incremental Delivery

1. Foundation (T001–T004) → Build passes ✓
2. US1: Temporal tabs (T005–T007) → Staff can see Upcoming/Ongoing/Past ✓
3. US2: Search + Status (T008–T011) → Staff can search and filter by status ✓
4. US3: Sortable columns (T012–T014) → Staff can sort the list ✓
5. US4: Pagination (T015–T017) → Long lists paginate correctly ✓
6. US5: Type + Tag filter (T018–T021) → Full filter set complete ✓
7. Polish (T022–T025) → Empty states + final QA ✓

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the same phase
- [Story] label maps task to specific user story for traceability
- `useCoursesFilter.ts` is the central hook — user stories build on it incrementally; avoid large rewrites
- All filter dimensions compose (AND logic): a course must satisfy temporal bucket AND search AND status AND type AND tags simultaneously
- Commit after each checkpoint to keep the branch clean and reviewable
