# Implementation Plan: Per-Participant Custom Field Values

**Branch**: `006-participant-custom-fields` | **Date**: 2026-04-03 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/006-participant-custom-fields/spec.md`

## Summary

Add tenant-configurable custom field definitions (e.g. "Deposit Paid", "Payment Status") and a mechanism for staff to assign fields per course and record values per enrolled participant. Field definitions are managed in Tenant Settings; field assignments are a Course configuration; field values are stored per enrollment (Registration), ensuring complete isolation between a participant's enrollment in different courses. The primary editing surface is the Course Roster (participant list), where staff toggles or sets values inline.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend), TypeScript 5.x / React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql) | Mantine v9, TanStack Query v5, react-i18next  
**Storage**: PostgreSQL via EF Core — **3 new tables** with **3 new migrations** (one per module)  
**Testing**: dotnet test (xUnit), Vitest / React Testing Library (frontend)  
**Target Platform**: Web service (Linux server) + Web application (modern browsers)  
**Project Type**: Modular monolith (backend) + SPA (frontend)  
**Performance Goals**: Inline field value saves must appear instant (optimistic UI); roster load with fields ≤ existing roster baseline  
**Constraints**: Cross-module references by value (Guid) only — no FK constraints across schemas; no shared DbContexts  
**Scale/Scope**: Typical tenant has 2–10 field definitions, 1–50 course field assignments, 10–500 participant field values per course

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | ✅ PASS | `CustomFieldDefinition`, `CourseFieldAssignment`, `ParticipantFieldValue` are proper domain entities in their respective bounded contexts. Business invariants (name uniqueness, value validation) enforced in domain layer. Repository interfaces defined in Domain. |
| II. Multi-Tenancy by Default | ✅ PASS | `CustomFieldDefinition` carries `TenantId`; `ParticipantFieldValue` carries `TenantId` for global query filter; `CourseFieldAssignment` inherits tenant scoping via Course. Cross-module references use `Guid` values only — no data crossing tenant boundaries. |
| III. Multi-Language First | ✅ PASS | No hardcoded user-facing strings. All UI labels, column headers, field type names, and error messages added to `en.json` and `cs.json` translation files. |
| IV. Clean Architecture | ✅ PASS | Dependency direction maintained: Infrastructure → Application → Domain. Domain entities and repository interfaces in Domain layer; handlers in Application layer; EF Core configurations and repository implementations in Infrastructure layer. API layer delegates to MediatR only. |
| V. Spec-First Development | ✅ PASS | `spec.md` authored and quality-validated before this plan was created. |

**Constitution Check Result: ALL GATES PASS — proceed to Phase 1.**

## Project Structure

### Documentation (this feature)

```text
specs/006-participant-custom-fields/
├── plan.md              ← This file
├── research.md          ← Phase 0: architecture decisions
├── data-model.md        ← Phase 1: entities, fields, indexes
├── quickstart.md        ← Phase 1: dev walkthrough
├── contracts/
│   └── api.md           ← Phase 1: REST API contracts
└── tasks.md             ← Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code (repository root)

```text
backend/
src/
├── Terminar.Modules.Tenants/
│   ├── Domain/
│   │   ├── CustomFieldDefinition.cs          ← NEW entity
│   │   ├── CustomFieldType.cs                ← NEW enum
│   │   ├── Events/
│   │   │   └── CustomFieldDefinitionDeleted.cs  ← NEW domain event
│   │   └── Repositories/
│   │       └── ICustomFieldDefinitionRepository.cs  ← NEW
│   ├── Application/
│   │   └── CustomFields/
│   │       ├── ListCustomFieldDefinitionsQuery.cs  ← NEW
│   │       ├── CreateCustomFieldDefinitionCommand.cs  ← NEW
│   │       ├── UpdateCustomFieldDefinitionCommand.cs  ← NEW
│   │       └── DeleteCustomFieldDefinitionCommand.cs  ← NEW
│   └── Infrastructure/
│       ├── Repositories/
│       │   └── CustomFieldDefinitionRepository.cs  ← NEW
│       └── Configurations/
│           └── CustomFieldDefinitionConfiguration.cs  ← NEW EF config
│
├── Terminar.Modules.Courses/
│   ├── Domain/
│   │   ├── Course.cs                         ← EXTEND (add CustomFieldAssignments collection + SetCustomFieldAssignments method)
│   │   ├── CourseFieldAssignment.cs          ← NEW entity
│   │   └── Repositories/
│   │       └── ICourseRepository.cs          ← EXTEND (no interface change needed — Course aggregate owns assignments)
│   ├── Application/
│   │   └── CustomFields/
│   │       ├── GetCourseCustomFieldsQuery.cs   ← NEW (returns definitions with assignment status)
│   │       └── UpdateCourseCustomFieldsCommand.cs  ← NEW
│   └── Infrastructure/
│       ├── CoursesDbContext.cs               ← EXTEND (add CourseFieldAssignment DbSet + configuration)
│       └── Configurations/
│           └── CourseFieldAssignmentConfiguration.cs  ← NEW EF config
│
├── Terminar.Modules.Registrations/
│   ├── Domain/
│   │   ├── Registration.cs                   ← EXTEND (add FieldValues collection + SetFieldValue method)
│   │   ├── ParticipantFieldValue.cs          ← NEW entity
│   │   └── Events/
│   │       └── ParticipantFieldValueUpdated.cs  ← NEW domain event
│   ├── Application/
│   │   ├── GetCourseRosterQuery.cs           ← EXTEND response to include customFieldValues + enabledCustomFields
│   │   └── CustomFields/
│   │       └── SetParticipantFieldValueCommand.cs  ← NEW
│   └── Infrastructure/
│       ├── RegistrationsDbContext.cs         ← EXTEND (add ParticipantFieldValue DbSet + configuration)
│       └── Configurations/
│           └── ParticipantFieldValueConfiguration.cs  ← NEW EF config
│
└── Terminar.Api/
    └── Modules/
        ├── CustomFieldsModule.cs             ← NEW (settings endpoints)
        ├── CoursesModule.cs                  ← EXTEND (add custom fields endpoints)
        └── RegistrationsModule.cs            ← EXTEND (add field-values PATCH endpoint)

frontend/
src/
├── features/
│   ├── settings/
│   │   ├── CustomFieldsSettingsPage.tsx      ← NEW
│   │   └── customFieldsApi.ts               ← NEW
│   ├── courses/
│   │   └── CourseCustomFieldsSection.tsx    ← NEW (shown in CourseDetailPage / EditCoursePage)
│   └── registrations/
│       ├── CourseRosterPage.tsx             ← EXTEND (dynamic custom field columns + inline edit)
│       └── registrationsApi.ts             ← EXTEND (field-values PATCH; extend roster response types)
└── shared/
    └── i18n/
        ├── locales/en.json                  ← EXTEND (add customFields.* keys)
        └── locales/cs.json                  ← EXTEND (add customFields.* keys)
```

**Structure Decision**: Option 2 (web application — backend + frontend). All new backend code follows the existing modular monolith pattern with one Application folder per feature area within each module. Three EF Core migrations required (one per DbContext).
