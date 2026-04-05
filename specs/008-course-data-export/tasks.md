# Tasks: Course Data Export

**Input**: Design documents from `/specs/008-course-data-export/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-export.md ✅, quickstart.md ✅

**Tests**: Not requested — no test tasks included.

**Organization**: Tasks grouped by user story for independent implementation and delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to
- Exact file paths included in every task description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Introduce the one new external dependency required before any implementation begins.

- [x] T001 Add `CsvHelper` NuGet package to `src/Terminar.Api/Terminar.Api.csproj` (run: `dotnet add src/Terminar.Api package CsvHelper`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core building blocks required by all user story phases. No user story work can begin until this phase is complete.

- [x] T002 Create `ExportColumnDefinition.cs` in `src/Terminar.Api/Services/ExportColumnDefinition.cs` — define `record ExportColumnDefinition(string Key, string LabelKey, ExportColumnGroup Group, bool DefaultEnabled, bool RequiresParticipants, string? Label = null)` and `enum ExportColumnGroup { CourseInfo, ParticipantInfo, CustomFields }`
- [x] T003 Create `CsvExportService.cs` skeleton in `src/Terminar.Api/Services/CsvExportService.cs` — define `ICsvExportService` interface with `BuildCoursesOnlyCsv` and `BuildWithParticipantsCsv` method signatures; add empty `CsvExportService : ICsvExportService` implementation; register as scoped service in `src/Terminar.Api/Program.cs`
- [x] T004 [P] Create `download.ts` in `frontend/src/lib/download.ts` — async `downloadFile(url: string, filename: string, params?: Record<string, string | string[]>): Promise<void>` that calls `fetch` with the current auth bearer token from auth store, receives `blob()`, creates an object URL, triggers an `<a>` click, and revokes the URL
- [x] T005 [P] Add export i18n keys to `frontend/src/i18n/en.json` and `frontend/src/i18n/cs.json` — keys for: `export.title`, `export.button`, `export.download`, `export.includeParticipants`, `export.dateFrom`, `export.dateTo`, `export.statusFilter`, `export.columns.title`, `export.columns.selectAll`, `export.columns.deselectAll`, `export.validation.noColumns`, `export.empty`, and all column label keys from `contracts/api-export.md`

**Checkpoint**: Foundation ready — user story phases can now begin.

---

## Phase 3: User Story 1 — Export Course List Without Participants (Priority: P1) 🎯 MVP

**Goal**: Staff can click Export on the courses list, configure date/status filters, and download a CSV of courses (no participant rows).

**Independent Test**: `GET /api/v1/courses/export?include_participants=false&columns=course_title&columns=course_status&columns=course_first_session_at&columns=course_enrolled_count` downloads a valid UTF-8 BOM CSV with one row per course and correct header names.

- [x] T006 [P] [US1] Create `ExportCourseRowDto.cs` in `src/Terminar.Modules.Courses/Application/Queries/ExportCourses/ExportCourseRowDto.cs` — record with fields: `Title`, `Description`, `CourseType` (string), `RegistrationMode` (string), `Capacity` (int), `Status` (string), `FirstSessionAt` (DateOnly?), `LastSessionEndsAt` (DateOnly?), `Location` (string?), `CourseId` (Guid, for cross-module count join); all string enum fields use `.ToString()`
- [x] T007 [P] [US1] Create `GetCourseEnrollmentCountsQuery.cs` and `GetCourseEnrollmentCountsHandler.cs` in `src/Terminar.Modules.Registrations/Application/Queries/GetCourseEnrollmentCounts/` — query takes `Guid TenantId` and `IReadOnlyList<Guid> CourseIds`; handler queries `Registrations` table with `WHERE TenantId = @tid AND CourseId IN (@ids)`, groups by `CourseId`, returns `Dictionary<Guid, (int Enrolled, int Waitlisted)>` (Enrolled = count of `Status == Confirmed`, Waitlisted = 0 until domain supports it)
- [x] T008 [US1] Create `ExportCoursesQuery.cs` in `src/Terminar.Modules.Courses/Application/Queries/ExportCourses/ExportCoursesQuery.cs` — record with `Guid TenantId`, `DateOnly? DateFrom`, `DateOnly? DateTo`, `CourseStatus? Status`; returns `List<ExportCourseRowDto>`
- [x] T009 [US1] Create `ExportCoursesHandler.cs` in `src/Terminar.Modules.Courses/Application/Queries/ExportCourses/ExportCoursesHandler.cs` — queries `CoursesDbContext.Courses.Include(c => c.Sessions).Where(c => c.TenantId == tid)`, applies optional date filters on first session's `ScheduledAt`, applies optional status filter; maps to `ExportCourseRowDto`; no pagination
- [x] T010 [US1] Implement `CsvExportService.BuildCoursesOnlyCsv(List<ExportCourseRowDto> courses, Dictionary<Guid, (int, int)> counts, IReadOnlyList<string> selectedColumns)` in `src/Terminar.Api/Services/CsvExportService.cs` — uses `CsvHelper.CsvWriter` with `StreamWriter` using `UTF8Encoding(encoderShouldEmitUTF8Identifier: true)`; writes only the selected columns using a dynamic column mapping; returns `byte[]`
- [x] T011 [US1] Add `GET /api/v1/courses/export/columns` endpoint to `src/Terminar.Api/Modules/CoursesModule.cs` — calls `IMediator` to fetch tenant custom fields via `ListCustomFieldDefinitionsQuery`; calls `IPluginActivationService` to check Excusals; builds and returns `ExportColumnDefinition[]` with all course info columns + conditional excusal_count + custom field columns
- [x] T012 [US1] Add `GET /api/v1/courses/export` endpoint (courses-only path) to `src/Terminar.Api/Modules/CoursesModule.cs` — reads query params `include_participants` (default false), `date_from`, `date_to`, `status`, `columns[]`; validates `columns` not empty (400); when `include_participants=false`: sends `ExportCoursesQuery` + `GetCourseEnrollmentCountsQuery`, calls `CsvExportService.BuildCoursesOnlyCsv()`, returns `Results.File(bytes, "text/csv; charset=utf-8", $"courses-export-{DateTime.Today:yyyy-MM-dd}.csv")`
- [x] T013 [P] [US1] Add `getExportColumns(): Promise<ExportColumnDefinition[]>` and `downloadCoursesExport(params: CoursesExportParams): Promise<void>` to `frontend/src/features/courses/coursesApi.ts` — `downloadCoursesExport` constructs the query string and calls `downloadFile()` from `lib/download.ts`; define `CoursesExportParams` and `ExportColumnDefinition` TypeScript types
- [x] T014 [US1] Create `ExportCoursesModal.tsx` in `frontend/src/features/courses/ExportCoursesModal.tsx` — Mantine `Modal` component; on open: calls `getExportColumns()` and shows loading state; renders date range pickers, status `MultiSelect`, and a list of course-info column checkboxes pre-checked to `defaultEnabled`; "Download CSV" button calls `downloadCoursesExport()` and closes modal
- [x] T015 [US1] Add "Export" button to `frontend/src/features/courses/CourseListPage.tsx` — place in page toolbar next to existing actions; on click opens `ExportCoursesModal`; no changes to existing filter/list logic

**Checkpoint**: US1 fully functional — staff can export course list as CSV from courses page.

---

## Phase 4: User Story 2 — Export With Participants (Priority: P2)

**Goal**: Staff can toggle "Include participants" in the export modal and download a flat CSV with one row per participant per course, including all custom fields.

**Independent Test**: `GET /api/v1/courses/export?include_participants=true&columns=course_title&columns=participant_name&columns=participant_email&columns=enrollment_status` downloads a CSV where each row is one participant with course columns repeated.

- [x] T016 [P] [US2] Create `ExportParticipantRowDto.cs` in `src/Terminar.Modules.Registrations/Application/Queries/ExportCourseRoster/ExportParticipantRowDto.cs` — record with: `CourseId` (Guid), `ParticipantName`, `ParticipantEmail`, `EnrollmentStatus` (string), `EnrollmentDate` (DateOnly), `CustomFieldValues` (Dictionary<Guid, string?>), `ExcusalCount` (int? — null when not applicable)
- [x] T017 [US2] Create `ExportCourseRosterQuery.cs` in `src/Terminar.Modules.Registrations/Application/Queries/ExportCourseRoster/ExportCourseRosterQuery.cs` — record with `Guid TenantId`, `IReadOnlyList<Guid> CourseIds`, `bool IncludeExcusalCounts`; returns `ExportCourseRosterResult` containing `List<ExportParticipantRowDto>` and `IReadOnlyList<EnabledCustomFieldDto>` (field definitions for column headers)
- [x] T018 [US2] Create `ExportCourseRosterHandler.cs` in `src/Terminar.Modules.Registrations/Application/Queries/ExportCourseRoster/ExportCourseRosterHandler.cs` — queries all `Registrations.Include(r => r.FieldValues).Where(r => r.TenantId == tid && courseIds.Contains(r.CourseId))` (no pagination); calls `GetCourseCustomFieldsQuery` cross-module per course (or batch by tenant); maps to `ExportParticipantRowDto`; if `IncludeExcusalCounts`: calls excusal count query via `IPluginActivationService`-guarded MediatR send
- [x] T019 [US2] Implement `CsvExportService.BuildWithParticipantsCsv(List<ExportCourseRowDto> courses, List<ExportParticipantRowDto> participants, IReadOnlyList<EnabledCustomFieldDto> customFields, Dictionary<Guid, (int, int)> counts, IReadOnlyList<string> selectedColumns)` in `src/Terminar.Api/Services/CsvExportService.cs` — joins courses + participants by `CourseId`; courses with zero participants emit one row with blank participant fields; custom field columns added dynamically using field definition IDs; selected columns filter applied
- [x] T020 [US2] Update `GET /api/v1/courses/export` endpoint in `src/Terminar.Api/Modules/CoursesModule.cs` — when `include_participants=true`: additionally sends `ExportCourseRosterQuery` with all matching course IDs, passes combined data to `CsvExportService.BuildWithParticipantsCsv()`, checks `IPluginActivationService` for excusal column inclusion
- [x] T021 [US2] Update `ExportCoursesModal.tsx` in `frontend/src/features/courses/ExportCoursesModal.tsx` — add `Switch` for "Include participants"; when toggled on: show `ParticipantInfo` and `CustomFields` column groups (filtered from `ExportColumnDefinition[]`); when toggled off: hide participant/custom-field columns; pass `include_participants` to `downloadCoursesExport()`

**Checkpoint**: US2 functional — flat per-participant export works with custom fields.

---

## Phase 5: User Story 3 — Export Single Course Participant Roster (Priority: P2)

**Goal**: Staff on a course detail/roster page can click "Export CSV" to download just that course's participants.

**Independent Test**: `GET /api/v1/courses/{courseId}/registrations/export?columns=participant_name&columns=participant_email&columns=enrollment_date` downloads a CSV with only that course's participants and correct columns.

- [x] T022 [US3] Add `GET /api/v1/courses/{courseId}/registrations/export` endpoint to `src/Terminar.Api/Modules/CoursesModule.cs` — validates course belongs to tenant (404 if not); reads `columns[]` query param (400 if empty); sends `ExportCourseRosterQuery` for that single courseId; calls `CsvExportService.BuildWithParticipantsCsv()` with participant columns only (course context omitted from output); returns `Results.File` with filename `course-{courseId}-participants-{date}.csv`
- [x] T023 [P] [US3] Create `ExportRosterModal.tsx` in `frontend/src/features/registrations/ExportRosterModal.tsx` — Mantine `Modal`; on open: calls `getExportColumns()` and shows only `ParticipantInfo` and `CustomFields` column groups; "Download CSV" button calls roster export download and closes; accepts `courseId` prop
- [x] T024 [P] [US3] Add `downloadRosterExport(courseId: string, params: RosterExportParams): Promise<void>` to `frontend/src/features/registrations/registrationsApi.ts` — calls `downloadFile()` for `GET /api/v1/courses/{courseId}/registrations/export` with column params
- [x] T025 [US3] Add "Export CSV" button to `frontend/src/features/registrations/CourseRosterPage.tsx` — place in page toolbar alongside existing actions; on click opens `ExportRosterModal` with current `courseId`

**Checkpoint**: US3 functional — single-course roster download works from course detail page.

---

## Phase 6: User Story 4 — Column Selection and Export Options (Priority: P3)

**Goal**: Staff can toggle individual columns on/off in the export modal; their choices persist for the session.

**Independent Test**: Deselecting "Email" in the modal and downloading produces a CSV with no email column; reopening the modal within the same session still shows Email unchecked.

- [x] T026 [US4] Update `ExportCoursesModal.tsx` in `frontend/src/features/courses/ExportCoursesModal.tsx` — replace flat checkbox list with grouped accordion sections (CourseInfo / ParticipantInfo / CustomFields using Mantine `Accordion`); add "Select all" / "Deselect all" per group; state initialized from `sessionStorage` key `export_columns_courses` if present, else from `defaultEnabled` values
- [x] T027 [P] [US4] Update `ExportRosterModal.tsx` in `frontend/src/features/registrations/ExportRosterModal.tsx` — same grouped column selection; state initialized from `sessionStorage` key `export_columns_roster`
- [x] T028 [US4] Add sessionStorage save on download/close in `ExportCoursesModal.tsx` — call `sessionStorage.setItem('export_columns_courses', JSON.stringify(selectedColumns))` before triggering download; also save `includeParticipants`, `dateFrom`, `dateTo`, `statusFilter`
- [x] T029 [P] [US4] Add sessionStorage save on download/close in `ExportRosterModal.tsx` — save selected columns to `sessionStorage.setItem('export_columns_roster', ...)`
- [x] T030 [US4] Add client-side no-columns validation in `ExportCoursesModal.tsx` and `ExportRosterModal.tsx` — disable "Download CSV" button and show inline error message (i18n key `export.validation.noColumns`) when no columns are checked
- [x] T031 [US4] Add server-side column validation to export endpoints in `src/Terminar.Api/Modules/CoursesModule.cs` — return `Results.BadRequest("At least one column must be selected")` if `columns` parameter is empty; silently ignore unknown column keys

**Checkpoint**: US4 functional — column selection, session persistence, and validation all work.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, conditional features, and final validation.

- [x] T032 [P] Add empty-result user feedback to `ExportCoursesModal.tsx` in `frontend/src/features/courses/ExportCoursesModal.tsx` — detect when downloaded CSV has only a header row (Content-Length hint or a preflight count endpoint) and show Mantine `Alert` with i18n key `export.empty`; alternatively show count in modal before download
- [x] T033 [P] Verify excusal count column in `ExportCourseRosterHandler.cs` — confirm `IPluginActivationService.IsActiveAsync("Excusals", tenantId)` is checked before including excusal counts; confirm column is absent from `GET /api/v1/courses/export/columns` response when plugin is inactive for the tenant
- [x] T034 [P] Verify date field formatting in `CsvExportService.cs` — confirm all `DateOnly` and `DateTime` values are serialized as `yyyy-MM-dd` (ISO 8601) using CsvHelper type converter configuration; verify no locale-dependent formatting
- [x] T035 Run quickstart.md validation scenarios — execute all curl commands from `specs/008-course-data-export/quickstart.md`; open downloaded CSVs in Excel/Google Sheets and verify character encoding, column counts, and RFC 4180 compliance

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user story phases
- **US1 (Phase 3)**: Depends on Phase 2 — can start immediately after Foundational
- **US2 (Phase 4)**: Depends on Phase 3 being complete (reuses `ExportCoursesHandler` + extends modal and API endpoint)
- **US3 (Phase 5)**: Depends on Phase 4 being complete (reuses `ExportCourseRosterHandler` from T018)
- **US4 (Phase 6)**: Depends on Phase 3 (ExportCoursesModal exists), T023 (ExportRosterModal exists) — best done after Phase 5
- **Polish (Phase 7)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Only depends on Foundational — this is the MVP
- **US2 (P2)**: Depends on US1 (extends same endpoint and modal)
- **US3 (P2)**: Depends on US2 (reuses `ExportCourseRosterHandler` created in T018)
- **US4 (P3)**: Depends on US1 and US3 modals existing (wraps UI already built)

### Within Each User Story

- DTOs before query/handler (T006 → T008, T007 → T009)
- Backend handler before API endpoint
- API endpoint before frontend integration
- Modal component before page button integration

### Parallel Opportunities

- T006 and T007 can run in parallel (different modules)
- T004 and T005 can run in parallel during Phase 2
- T013 and T014 can run in parallel (different files, no dependency between them)
- T016 and T017 can start in parallel (T016 is a DTO, T017 is the query record — both needed by T018)
- T023 and T024 can run in parallel
- T027, T029, T033, T034 can all run in parallel during Phase 7

---

## Parallel Example: User Story 1

```
# Parallel batch 1 (after Foundational phase):
Task T006: Create ExportCourseRowDto.cs
Task T007: Create GetCourseEnrollmentCountsQuery + Handler

# Sequential after T006+T007:
Task T008: Create ExportCoursesQuery.cs
Task T009: Create ExportCoursesHandler.cs  (depends on T006)

# After T009:
Task T010: BuildCoursesOnlyCsv() in CsvExportService
Task T011: GET /export/columns endpoint

# After T010 + T011:
Task T012: GET /export endpoint (courses-only)

# Parallel (can start after T012):
Task T013: Frontend API functions (coursesApi.ts)
Task T014: ExportCoursesModal.tsx

# After T013 + T014:
Task T015: Add Export button to CourseListPage.tsx
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T005)
3. Complete Phase 3: US1 (T006–T015)
4. **STOP and VALIDATE**: Export courses list as CSV, open in Excel, verify encoding
5. Demo to stakeholders

### Incremental Delivery

1. Phase 1 + Phase 2 → Infrastructure ready
2. Phase 3 → **MVP**: Courses-only CSV export with filters ✓
3. Phase 4 → **Enhancement**: Add participants to export ✓
4. Phase 5 → **Enhancement**: Per-course roster export from detail page ✓
5. Phase 6 → **Enhancement**: Full column selection + session persistence ✓
6. Phase 7 → Polish and edge cases ✓

---

## Notes

- [P] tasks = different files, no incomplete task dependencies
- [Story] label maps each task to its user story for traceability
- CsvHelper must be added (T001) before any backend CSV work can compile
- `ExportCourseRosterHandler` (T018) is shared by both US2 and US3 — implement once in US2 phase
- Enrolled/waitlisted counts (T007) are a cross-module read — uses `GetCourseEnrollmentCountsQuery` dispatched by the API endpoint orchestrator, not inside the Courses module handler
- Column selection UI (Phase 6) is intentionally layered on top of basic modal UI (Phase 3/5) rather than built all at once
- Commit after each checkpoint (end of Phase 3, 4, 5, 6)
