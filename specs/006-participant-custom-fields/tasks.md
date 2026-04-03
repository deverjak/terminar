# Tasks: Per-Participant Custom Field Values (006)

**Input**: Design documents from `/specs/006-participant-custom-fields/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api.md ✅, quickstart.md ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. No test tasks are generated (not requested in spec).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared state)
- **[Story]**: Maps to user story from spec.md (US1–US4)
- Paths assume modular monolith layout per plan.md

---

## Phase 1: Foundational Domain Entities

**Purpose**: Create domain-layer entities and value types that ALL user stories depend on. No user story work can begin until this phase is complete.

**⚠️ CRITICAL**: Runs before any user story phase. All three modules are independent — Tenants, Courses, and Registrations entity tasks can be done in parallel.

- [X] T001 [P] Create `CustomFieldType` enum (`YesNo`, `Text`, `OptionsList`) in `src/Terminar.Modules.Tenants/Domain/CustomFieldType.cs`
- [X] T002 [P] Create `CustomFieldDefinition` entity with fields `Id`, `TenantId`, `Name`, `FieldType`, `AllowedValues`, `DisplayOrder`, `CreatedAt` and factory method `Create(tenantId, name, fieldType, allowedValues)` enforcing name-non-empty and OptionsList-requires-values invariants in `src/Terminar.Modules.Tenants/Domain/CustomFieldDefinition.cs`
- [X] T003 [P] Create `ICustomFieldDefinitionRepository` interface with `ListByTenantAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` in `src/Terminar.Modules.Tenants/Domain/Repositories/ICustomFieldDefinitionRepository.cs`
- [X] T004 [P] Create `CourseFieldAssignment` entity with fields `Id`, `CourseId`, `FieldDefinitionId`, `DisplayOrder` in `src/Terminar.Modules.Courses/Domain/CourseFieldAssignment.cs`
- [X] T005 [P] Create `ParticipantFieldValue` entity with fields `Id`, `RegistrationId`, `TenantId`, `FieldDefinitionId`, `Value` (nullable string), `UpdatedAt` in `src/Terminar.Modules.Registrations/Domain/ParticipantFieldValue.cs`

**Checkpoint**: Domain entities exist in code — EF configurations and migrations can now be created.

---

## Phase 2: EF Core Configurations & Migrations

**Purpose**: Persist the new domain entities. All three migrations are independent and can run in parallel.

**⚠️ CRITICAL**: Must complete before any application-layer work can be tested end-to-end.

- [X] T006 [P] Create `CustomFieldDefinitionConfiguration` (table `tenants.custom_field_definitions`, unique index on `(tenant_id, name)`, `allowed_values` as `text[]`, TenantId value-object conversion) in `src/Terminar.Modules.Tenants/Infrastructure/Configurations/CustomFieldDefinitionConfiguration.cs`; add `DbSet<CustomFieldDefinition>` to `src/Terminar.Modules.Tenants/Infrastructure/TenantsDbContext.cs`; generate migration `AddCustomFieldDefinitions`
- [X] T007 [P] Create `CourseFieldAssignmentConfiguration` (table `courses.course_field_assignments`, unique index on `(course_id, field_definition_id)`) in `src/Terminar.Modules.Courses/Infrastructure/Configurations/CourseFieldAssignmentConfiguration.cs`; add `DbSet<CourseFieldAssignment>` to `src/Terminar.Modules.Courses/Infrastructure/CoursesDbContext.cs`; generate migration `AddCourseFieldAssignments`
- [X] T008 [P] Create `ParticipantFieldValueConfiguration` (table `registrations.participant_field_values`, unique index on `(registration_id, field_definition_id)`, non-unique index on `tenant_id`, TenantId value-object conversion) in `src/Terminar.Modules.Registrations/Infrastructure/Configurations/ParticipantFieldValueConfiguration.cs`; add `DbSet<ParticipantFieldValue>` to `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsDbContext.cs`; generate migration `AddParticipantFieldValues`

**Checkpoint**: `dotnet run` starts without migration errors. Tables exist in the database.

---

## Phase 3: User Story 1 — Configure Custom Fields in Tenant Settings (Priority: P1) 🎯 MVP

**Goal**: Tenant admins can create, edit, and delete custom field definitions (e.g. "Deposit Paid" Yes/No) in Tenant Settings.

**Independent Test**: Navigate to Tenant Settings, create two field definitions, edit one name, delete the other — verify all operations work and field names are unique per tenant.

### Backend — Repository & Application Layer

- [X] T009 [P] [US1] Implement `CustomFieldDefinitionRepository` with `ListByTenantAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` using EF Core; register in DI in `src/Terminar.Modules.Tenants/Infrastructure/Repositories/CustomFieldDefinitionRepository.cs`
- [X] T010 [P] [US1] Create `ListCustomFieldDefinitionsQuery` record and `ListCustomFieldDefinitionsHandler` (returns ordered list by `DisplayOrder` filtered by TenantId) in `src/Terminar.Modules.Tenants/Application/CustomFields/ListCustomFieldDefinitionsQuery.cs`
- [X] T011 [P] [US1] Create `CreateCustomFieldDefinitionCommand` record, `CreateCustomFieldDefinitionValidator` (name required, max 100 chars; `AllowedValues` non-empty when OptionsList; unique name check), and `CreateCustomFieldDefinitionHandler` in `src/Terminar.Modules.Tenants/Application/CustomFields/CreateCustomFieldDefinitionCommand.cs`
- [X] T012 [P] [US1] Create `UpdateCustomFieldDefinitionCommand` record, `UpdateCustomFieldDefinitionValidator`, and `UpdateCustomFieldDefinitionHandler` (updates name and/or allowedValues; fieldType immutable) in `src/Terminar.Modules.Tenants/Application/CustomFields/UpdateCustomFieldDefinitionCommand.cs`
- [X] T013 [P] [US1] Create `DeleteCustomFieldDefinitionCommand` record and `DeleteCustomFieldDefinitionHandler` (hard-delete; raises `CustomFieldDefinitionDeleted` domain event placeholder for Phase 7) in `src/Terminar.Modules.Tenants/Application/CustomFields/DeleteCustomFieldDefinitionCommand.cs`
- [X] T014 [US1] Create `CustomFieldsModule` with Minimal API endpoints: `GET /api/v1/settings/custom-fields`, `POST /api/v1/settings/custom-fields`, `PATCH /api/v1/settings/custom-fields/{fieldId}`, `DELETE /api/v1/settings/custom-fields/{fieldId}` (all require Staff auth; delegate to MediatR); register in `Program.cs` in `src/Terminar.Api/Modules/CustomFieldsModule.cs`

### Frontend — Settings Page

- [X] T015 [P] [US1] Create `customFieldsApi.ts` with `listCustomFields()`, `createCustomField(data)`, `updateCustomField(id, data)`, `deleteCustomField(id)` using `apiFetch`; include TypeScript types `CustomFieldDefinition`, `CustomFieldType`, `CreateCustomFieldRequest`, `UpdateCustomFieldRequest` in `frontend/src/features/settings/customFieldsApi.ts`
- [X] T016 [US1] Create `CustomFieldsSettingsPage.tsx` — table listing field definitions with columns (Name, Type, Allowed Values, Actions); "Add Field" button opens modal with name input, type selector, and conditional allowed-values list editor; inline Edit/Delete per row; uses TanStack Query with `useQuery`/`useMutation` from T015 in `frontend/src/features/settings/CustomFieldsSettingsPage.tsx`
- [X] T017 [P] [US1] Add `customFields.*` translation keys to English in `frontend/src/shared/i18n/locales/en.json` (keys: `customFields.title`, `customFields.addField`, `customFields.fieldName`, `customFields.fieldType`, `customFields.allowedValues`, `customFields.typeYesNo`, `customFields.typeText`, `customFields.typeOptionsList`, `customFields.deleteConfirm`, `customFields.nameDuplicate`, error messages)
- [X] T018 [P] [US1] Add `customFields.*` translation keys to Czech in `frontend/src/shared/i18n/locales/cs.json` (same keys as T017 in Czech)

**Checkpoint**: `GET/POST/PATCH/DELETE /api/v1/settings/custom-fields` all work. Tenant Settings page shows field list and all CRUD operations succeed.

---

## Phase 4: User Story 2 — Assign Custom Fields to a Course (Priority: P1)

**Goal**: Staff can enable or disable specific custom fields per course from course settings so each course shows only relevant fields.

**Independent Test**: Create two courses; enable "Deposit Paid" on Course A only; verify Course A's assignment list shows it enabled and Course B's shows it disabled.

### Backend — Course Aggregate & Application Layer

- [X] T019 [US2] Extend `Course` aggregate: add `CustomFieldAssignments: IReadOnlyList<CourseFieldAssignment>` collection (EF navigation), add `SetCustomFieldAssignments(IEnumerable<Guid> fieldDefinitionIds)` method that replaces the list and assigns `DisplayOrder` from input index in `src/Terminar.Modules.Courses/Domain/Course.cs`
- [X] T020 [P] [US2] Create `GetCourseCustomFieldsQuery` record and `GetCourseCustomFieldsHandler` — dispatches `ListCustomFieldDefinitionsQuery` (Tenants module) for tenant's definitions AND reads `Course.CustomFieldAssignments` from Courses module; merges into a combined response with `IsEnabled` flag per definition in `src/Terminar.Modules.Courses/Application/CustomFields/GetCourseCustomFieldsQuery.cs`
- [X] T021 [P] [US2] Create `UpdateCourseCustomFieldsCommand` record, `UpdateCourseCustomFieldsValidator` (all field IDs must belong to the tenant — cross-validates via `ListCustomFieldDefinitionsQuery`), and `UpdateCourseCustomFieldsHandler` (calls `Course.SetCustomFieldAssignments`) in `src/Terminar.Modules.Courses/Application/CustomFields/UpdateCourseCustomFieldsCommand.cs`
- [X] T022 [US2] Register course custom fields endpoints in `src/Terminar.Api/Modules/CoursesModule.cs`: `GET /api/v1/courses/{courseId}/custom-fields` and `PUT /api/v1/courses/{courseId}/custom-fields` (Staff auth; delegate to MediatR)

### Frontend — Course Settings Section

- [X] T023 [US2] Create `CourseCustomFieldsSection.tsx` — fetches course's custom fields via `GET /api/v1/courses/{id}/custom-fields`; renders checklist of all tenant fields with toggles showing `isEnabled`; on change calls `PUT /api/v1/courses/{id}/custom-fields` with full updated list; add API functions `getCourseCustomFields(courseId)` and `updateCourseCustomFields(courseId, data)` to `frontend/src/features/courses/coursesApi.ts` in `frontend/src/features/courses/CourseCustomFieldsSection.tsx`
- [X] T024 [US2] Integrate `CourseCustomFieldsSection` into `CourseDetailPage.tsx` or `EditCoursePage.tsx` (whichever is the course settings surface); add section heading and conditional render (show only when tenant has ≥1 field definition) in `frontend/src/features/courses/CourseDetailPage.tsx`
- [X] T025 [P] [US2] Add course custom fields i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` (keys: `courses.customFields.sectionTitle`, `courses.customFields.enabledToggleLabel`, `courses.customFields.noFieldsDefined`, `courses.customFields.saveSuccess`)

**Checkpoint**: `GET /api/v1/courses/{id}/custom-fields` returns definitions with `isEnabled`. `PUT` updates assignments. Course settings UI shows toggle list and saves correctly.

---

## Phase 5: User Story 3 — Record Field Values Per Enrolled Participant (Priority: P1)

**Goal**: Staff can see custom field columns per enabled field in the participant list and update values inline (auto-save on toggle/selection/blur).

**Independent Test**: Open a course with "Deposit Paid" enabled, toggle a participant's value to "Yes", reload — value persists. Same participant in a different course shows independent value.

### Backend — Registration Aggregate & Application Layer

- [X] T026 [US3] Extend `Registration` aggregate: add `FieldValues: IReadOnlyList<ParticipantFieldValue>` collection (EF navigation); add `SetFieldValue(Guid fieldDefinitionId, string? value)` method (upsert — create if not exists, update if exists; set `UpdatedAt`; raise `ParticipantFieldValueUpdated` domain event) in `src/Terminar.Modules.Registrations/Domain/Registration.cs`
- [X] T027 [P] [US3] Create `ParticipantFieldValueUpdated` domain event record with `RegistrationId`, `FieldDefinitionId`, `Value`, `TenantId` in `src/Terminar.Modules.Registrations/Domain/Events/ParticipantFieldValueUpdated.cs`
- [X] T028 [P] [US3] Create `SetParticipantFieldValueCommand` record, `SetParticipantFieldValueValidator` (fieldDefinitionId must be enabled for the course; value must match allowed options for OptionsList type), and `SetParticipantFieldValueHandler` (loads `Registration`, calls `SetFieldValue`, saves) in `src/Terminar.Modules.Registrations/Application/CustomFields/SetParticipantFieldValueCommand.cs`
- [X] T029 [US3] Extend `GetCourseRosterQuery` response: add `CustomFieldValues: Dictionary<Guid, string?>` per registration row (keyed by `fieldDefinitionId`) and `EnabledCustomFields: List<CustomFieldDefinitionDto>` on the page-level response; handler fetches `ParticipantFieldValue` records via EF join on registration IDs and fetches enabled field definitions from Courses module (via `GetCourseCustomFieldsQuery`) in `src/Terminar.Modules.Registrations/Application/GetCourseRosterQuery.cs`
- [X] T030 [US3] Register field-values endpoint in `src/Terminar.Api/Modules/RegistrationsModule.cs`: `PATCH /api/v1/courses/{courseId}/registrations/{registrationId}/field-values` (Staff auth; body: `{ fieldDefinitionId, value }`; delegate to `SetParticipantFieldValueCommand`)

### Frontend — Roster Dynamic Columns & Inline Editing

- [X] T031 [US3] Extend TypeScript types in `frontend/src/features/registrations/registrationsApi.ts`: add `customFieldValues: Record<string, string | null>` to `Registration` type; add `enabledCustomFields: CustomFieldDefinitionDto[]` to `RosterPage` type; add `setParticipantFieldValue(courseId, registrationId, data)` API function
- [X] T032 [US3] Extend `CourseRosterPage.tsx`: dynamically generate table columns from `enabledCustomFields` in roster response; render `YesNo` field as `Checkbox`/`Switch` (Mantine); render `OptionsList` field as inline `Select`; render `Text` field as inline `TextInput`; each change calls `setParticipantFieldValue` with optimistic UI update; show `—` when value is null in `frontend/src/features/registrations/CourseRosterPage.tsx`
- [X] T033 [P] [US3] Add participant roster custom fields i18n keys to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` (keys: `registrations.fieldValue.notSet`, `registrations.fieldValue.saveError`, `registrations.fieldValue.yesLabel`, `registrations.fieldValue.noLabel`)

**Checkpoint**: Roster response includes `enabledCustomFields` and `customFieldValues`. Inline toggle/select saves immediately. Reload confirms persistence. Two enrollments for same participant have independent values.

---

## Phase 6: User Story 4 — Summary Counts in Participant List (Priority: P3)

**Goal**: Column header or footer shows "X / Y set" count for each custom field, letting staff assess progress at a glance.

**Independent Test**: Enroll 10 participants, set 3 values for "Deposit Paid = Yes" — verify header shows "3 / 10".

### Backend

- [X] T034 [US4] Extend `GetCourseRosterQuery` response: add `FieldValueSummary: Dictionary<Guid, int>` (count of non-null values per fieldDefinitionId) alongside existing roster data; compute with a group-by aggregate query over `participant_field_values` filtered to the course's registration IDs in `src/Terminar.Modules.Registrations/Application/GetCourseRosterQuery.cs`

### Frontend

- [X] T035 [US4] Extend `CourseRosterPage.tsx`: render summary count below each custom field column header as `"X / Y"` text using `FieldValueSummary` from roster response; update optimistically when a value changes in `frontend/src/features/registrations/CourseRosterPage.tsx`
- [X] T036 [P] [US4] Add summary count i18n key to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json` (key: `registrations.fieldValue.summary` with interpolation `"{{set}} / {{total}}"`)

**Checkpoint**: Summary row visible under custom field columns. Count updates immediately on value change.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Domain event wiring, cleanup of orphaned data, and end-to-end validation.

- [X] T037 [P] Create `CustomFieldDefinitionDeleted` domain event record with `FieldDefinitionId`, `TenantId` in `src/Terminar.Modules.Tenants/Domain/Events/CustomFieldDefinitionDeleted.cs`; raise it from `DeleteCustomFieldDefinitionHandler` (T013)
- [X] T038 [P] Create `OnCustomFieldDefinitionDeleted` notification handler in `src/Terminar.Modules.Courses/Application/CustomFields/` that listens for `CustomFieldDefinitionDeleted` and bulk-deletes all `CourseFieldAssignment` records with the matching `FieldDefinitionId`
- [ ] T039 Verify end-to-end quickstart walkthrough from `specs/006-participant-custom-fields/quickstart.md`: create 2 fields, assign to courses differently, set values, delete field definition — confirm all steps complete without errors
- [ ] T040 [P] Confirm all custom field columns in the participant list are absent when no fields are enabled for a course (regression test: existing roster view unchanged)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Domain Entities)
  ↓ (domain types must exist before EF config)
Phase 2 (EF Configs + Migrations)
  ↓ (database tables must exist before handlers can run)
Phase 3 (US1: Tenant Field Definitions)
  ↓ (field definitions must exist before they can be assigned)
Phase 4 (US2: Course Field Assignments)
  ↓ (assignments must exist before values can be set or roster extended)
Phase 5 (US3: Per-Participant Field Values + Roster)
  ↓ (values must exist before counts make sense)
Phase 6 (US4: Summary Counts)
  ↓
Phase 7 (Polish)
```

### User Story Dependencies

- **US1 (P1)**: Depends on Phases 1–2. No dependency on other stories.
- **US2 (P1)**: Depends on US1 (field definitions must exist to assign them to courses).
- **US3 (P1)**: Depends on US2 (fields must be enabled on a course before values can be set).
- **US4 (P3)**: Depends on US3 (values must exist to summarize).

### Within Each Phase

- Domain entity tasks [P] → run in parallel (different files in different modules)
- EF config tasks [P] → run in parallel per module
- Application-layer commands [P] → run in parallel (different command files)
- API registration tasks → sequential after all commands are ready
- Frontend API tasks [P] → run in parallel with backend when contracts are stable
- Translation tasks (en.json / cs.json) → en and cs can run in parallel [P]; different stories are sequential to the same file

### Parallel Opportunities

```
# Phase 1 — all 5 tasks in parallel:
T001 (CustomFieldType), T002 (CustomFieldDefinition), T003 (IRepository)
T004 (CourseFieldAssignment), T005 (ParticipantFieldValue)

# Phase 2 — all 3 EF tasks in parallel:
T006 (Tenants migration), T007 (Courses migration), T008 (Registrations migration)

# Phase 3 — repository + 4 command tasks in parallel:
T009 (Repository), T010 (List query), T011 (Create cmd), T012 (Update cmd), T013 (Delete cmd)
# Then: T014 (API module) after T009-T013
# Frontend parallel: T015 (API client) + T017 (en.json) + T018 (cs.json) in parallel
# Then: T016 (Settings page) after T015

# Phase 4 — application tasks in parallel:
T020 (GetCourseCustomFieldsQuery), T021 (UpdateCourseCustomFieldsCommand)
# Then: T022 (API endpoint) after T020-T021

# Phase 7:
T037 (domain event), T038 (deletion handler), T040 (regression check) — all parallel
```

---

## Parallel Example: Phase 3 (US1)

```
Step 1 — Launch all application tasks simultaneously:
  Task T009: Implement CustomFieldDefinitionRepository
  Task T010: Create ListCustomFieldDefinitionsQuery
  Task T011: Create CreateCustomFieldDefinitionCommand
  Task T012: Create UpdateCustomFieldDefinitionCommand
  Task T013: Create DeleteCustomFieldDefinitionCommand

Step 2 — After T009–T013 complete, launch:
  Task T014: Register API endpoints in CustomFieldsModule

Step 3 — In parallel with Step 1/2, launch frontend:
  Task T015: Create customFieldsApi.ts
  Task T017: Add en.json translations
  Task T018: Add cs.json translations

Step 4 — After T015 completes:
  Task T016: Build CustomFieldsSettingsPage
```

---

## Implementation Strategy

### MVP First (User Stories 1–3 Only)

1. Complete Phase 1: Domain entities
2. Complete Phase 2: EF migrations
3. Complete Phase 3: US1 (tenant settings CRUD) → **validate independently**
4. Complete Phase 4: US2 (course field assignments) → **validate independently**
5. Complete Phase 5: US3 (per-participant values + roster) → **validate independently**
6. **STOP and VALIDATE**: Run full quickstart.md walkthrough
7. Deploy/demo — core feature complete

### Incremental Delivery

1. Phases 1–2 → infrastructure ready
2. Phase 3 (US1) → admins can define fields, no participant tracking yet
3. Phase 4 (US2) → fields can be scoped to specific courses
4. Phase 5 (US3) → full per-participant tracking operational
5. Phase 6 (US4) → summary counts for convenience
6. Phase 7 → event-driven cleanup, polish

### Parallel Team Strategy

With two developers:
- Developer A: Backend (T001–T005 → T006–T008 → T009–T014 → T019–T022 → T026–T030 → T034 → T037–T038)
- Developer B: Frontend (T015–T018 → T023–T025 → T031–T033 → T035–T036 → T039–T040)

Backend can be 1–2 phases ahead of frontend once contracts are stable (use contracts/api.md as interface contract).

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps each task to its user story for traceability
- Translations (en.json / cs.json) for the same story can be done in parallel [P]; for different stories they are sequential to the same file
- No test tasks generated (tests not requested in spec.md)
- All cross-module references use plain `Guid` — never import domain types from another module
- Commit after each phase checkpoint at minimum
- Stop at the Phase 5 checkpoint to demo/validate before implementing Phase 6 (P3 story)
