# Research: Per-Participant Custom Field Values (006)

## Decision 1: Module Ownership of CustomFieldDefinition

**Decision**: `CustomFieldDefinition` lives in **Terminar.Modules.Tenants**

**Rationale**: A field definition is tenant configuration — it exists independently of any specific course and is managed in "Tenant Settings". The Tenants module already owns `TenantExcusalSettings` (a similar tenant-scoped configuration concept), making this a natural fit. The entity is created/updated/deleted by tenant admins, not by course staff.

**Alternatives considered**:
- Shared module (rejected — no shared module exists; cross-module is by value only)
- Courses module (rejected — field definitions are not course-specific; they're reusable across all courses within a tenant)

---

## Decision 2: Module Ownership of CourseFieldAssignment

**Decision**: `CourseFieldAssignment` lives in **Terminar.Modules.Courses**

**Rationale**: A course field assignment controls which fields are active on a course. It is a course configuration concern, analogous to `CourseExcusalPolicy` which already lives in the Courses module. It references `CustomFieldDefinitionId` as a plain `Guid` (cross-module reference by value — no FK across schemas).

**Alternatives considered**:
- Tenants module (rejected — it's a property of a course, not of the tenant)

---

## Decision 3: Module Ownership of ParticipantFieldValue

**Decision**: `ParticipantFieldValue` lives in **Terminar.Modules.Registrations**

**Rationale**: A field value is attached to a `Registration` (enrollment). When a registration is cancelled/deleted, its field values must also be deleted — this lifecycle coupling requires co-location in the Registrations module. The value stores `FieldDefinitionId: Guid` as a cross-module reference.

**Alternatives considered**:
- Separate module (rejected — overkill; tight lifecycle coupling to Registration)

---

## Decision 4: Value Storage Strategy

**Decision**: All field values stored as `string?` regardless of field type

**Rationale**: Avoids polymorphic column types in PostgreSQL. Serialization rules:
- `YesNo` field: store as `"true"` or `"false"` (or `null` = not yet set)
- `Text` field: store raw string (or `null`)
- `OptionsList` field: store the selected option string (or `null`)

**Alternatives considered**:
- JSONB column (rejected — adds query complexity for simple values)
- EF Core Table-Per-Type (rejected — over-engineered for 3 simple types)

---

## Decision 5: Cross-Module Data Composition for Roster View

**Decision**: API layer composes roster data + field values via two separate MediatR queries dispatched in the same handler

**Rationale**: The existing `GetCourseRosterQuery` (Registrations module) returns paginated registrations. A new `GetCourseCustomFieldsQuery` (Courses module) returns the enabled field definitions for a course. The Registrations roster response is extended to include `CustomFieldValues` per registration row. The handler fetches both and merges.

**Alternative**: Extend registrations query to call the courses repository directly (rejected — violates cross-module isolation)

---

## Decision 6: EF Core Migration Strategy

**Decision**: Three new migrations across three separate DbContexts

| Module | DbContext | New Tables |
|--------|-----------|-----------|
| Tenants | TenantsDbContext | `tenants.custom_field_definitions` |
| Courses | CoursesDbContext | `courses.course_field_assignments` |
| Registrations | RegistrationsDbContext | `registrations.participant_field_values` |

No FK constraints across schemas (cross-module by value). Application-layer consistency enforced by handler logic.

---

## Decision 7: Roster API Extension Strategy

**Decision**: Extend the existing `GET /api/v1/courses/{courseId}/registrations` response to include `customFieldValues` per registration row, and add a new field-values PATCH endpoint per registration

**Rationale**: The participant list is the primary editing surface. Including field values in the roster response avoids a second round-trip. The roster already accepts query params (page, pageSize, status) so the response shape extension is backward-compatible (existing consumers ignore unknown fields).

---

## Decision 8: Tenant Settings API URL

**Decision**: Custom field definition endpoints live under `/api/v1/settings/custom-fields`

**Rationale**: Tenant context is established via `TenantResolutionMiddleware` (same as all other endpoints). There is no explicit `tenantId` in existing endpoint URLs — the tenant is derived from the authenticated request. Placing under `/settings/` creates a logical grouping for future tenant settings endpoints.

---

## Decision 9: Course Field Assignment API

**Decision**: `GET /api/v1/courses/{courseId}/custom-fields` returns definitions with assignment status; `PUT` replaces the full assignment list

**Rationale**: A simple list-replace pattern (`PUT` with array of enabled field IDs) is simpler to implement and reason about than fine-grained add/remove operations. Course custom field configuration is an infrequent admin action so optimistic locking is not required.

---

## Resolved: No Clarifications Remaining

All architectural questions resolved through codebase research and existing patterns. No external dependencies or third-party integrations required.
