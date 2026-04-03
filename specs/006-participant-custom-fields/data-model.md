# Data Model: Per-Participant Custom Field Values (006)

## New Entities

---

### 1. CustomFieldDefinition

**Module**: `Terminar.Modules.Tenants`  
**Schema**: `tenants`  
**Table**: `custom_field_definitions`  
**Type**: Entity (not an aggregate root — owned by Tenant configuration)

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | PK, generated |
| `TenantId` | `TenantId` (→ Guid) | FK via value object conversion; global query filter |
| `Name` | `string` | Max 100 chars; unique per tenant |
| `FieldType` | `CustomFieldType` (enum) | `YesNo`, `Text`, `OptionsList` |
| `AllowedValues` | `List<string>` | Stored as `text[]`; only populated for `OptionsList` type |
| `DisplayOrder` | `int` | Controls column ordering in participant list |
| `CreatedAt` | `DateTimeOffset` | Set on creation, immutable |

**Enum: CustomFieldType**
```
YesNo       = 0
Text        = 1
OptionsList = 2
```

**Invariants**:
- `Name` must be non-empty and unique within the tenant.
- `AllowedValues` must have at least 1 entry when `FieldType = OptionsList`.
- `AllowedValues` must be empty when `FieldType ≠ OptionsList`.

**Relationships**:
- Belongs to one `Tenant` (via `TenantId`)
- Referenced by `CourseFieldAssignment.FieldDefinitionId` (cross-module, no FK constraint)
- Referenced by `ParticipantFieldValue.FieldDefinitionId` (cross-module, no FK constraint)

---

### 2. CourseFieldAssignment

**Module**: `Terminar.Modules.Courses`  
**Schema**: `courses`  
**Table**: `course_field_assignments`  
**Type**: Entity (child of Course aggregate)

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | PK, generated |
| `CourseId` | `Guid` | FK to `courses.courses.id` |
| `FieldDefinitionId` | `Guid` | Cross-module reference; no FK constraint |
| `DisplayOrder` | `int` | Controls column order within this course |

**Invariants**:
- A `(CourseId, FieldDefinitionId)` pair must be unique — no duplicate assignments.
- `FieldDefinitionId` is treated as an opaque identifier; the Courses module does not validate its existence in the Tenants module.

**Relationships**:
- Belongs to `Course` (child entity, owned via `Course.CustomFieldAssignments`)
- References `CustomFieldDefinition` by `FieldDefinitionId` (cross-module by value)

---

### 3. ParticipantFieldValue

**Module**: `Terminar.Modules.Registrations`  
**Schema**: `registrations`  
**Table**: `participant_field_values`  
**Type**: Entity (child of Registration aggregate)

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | PK, generated |
| `RegistrationId` | `Guid` | FK to `registrations.registrations.id` |
| `TenantId` | `TenantId` (→ Guid) | Global query filter; denormalized for tenant isolation |
| `FieldDefinitionId` | `Guid` | Cross-module reference; no FK constraint |
| `Value` | `string?` | Nullable; serialized string representation of the typed value |
| `UpdatedAt` | `DateTimeOffset` | Updated on every write |

**Value serialization rules**:
- `YesNo`: `"true"` / `"false"` / `null` (not yet set)
- `Text`: raw string / `null`
- `OptionsList`: selected option string / `null`

**Invariants**:
- A `(RegistrationId, FieldDefinitionId)` pair must be unique — one value record per field per enrollment.
- `TenantId` must match the tenant of the associated `Registration`.

**Lifecycle**:
- Created on first write (upsert pattern — create if not exists, update if exists).
- Deleted when the parent `Registration` is cancelled or removed.

**Relationships**:
- Belongs to `Registration` (child entity)
- References `CustomFieldDefinition` by `FieldDefinitionId` (cross-module by value)

---

## Affected Existing Entities

### Course (Terminar.Modules.Courses)

New collection added:

```
CustomFieldAssignments: IReadOnlyList<CourseFieldAssignment>
```

Method added:
- `SetCustomFieldAssignments(IEnumerable<Guid> fieldDefinitionIds)` — replaces the assignment list, reassigns `DisplayOrder` based on input order.

### Registration (Terminar.Modules.Registrations)

New collection added:

```
FieldValues: IReadOnlyList<ParticipantFieldValue>
```

Method added:
- `SetFieldValue(Guid fieldDefinitionId, string? value)` — upserts a field value; raises `ParticipantFieldValueUpdated` domain event.

---

## Domain Events

| Event | Raised By | Consumed By |
|-------|-----------|-------------|
| `CustomFieldDefinitionDeleted` | Tenant aggregate (or application service) | Courses module (to remove orphaned assignments), Registrations module (informational) |
| `ParticipantFieldValueUpdated` | Registration aggregate | (Audit log only for now; P3 summary counts may consume) |

**Note**: `CustomFieldDefinitionDeleted` uses the existing cross-module event dispatch pattern via MediatR `INotification`. The Courses module handler removes matching `CourseFieldAssignment` records; the Registrations module can optionally tombstone values.

---

## State Transitions: CustomFieldDefinition

```
Created → Active → Deleted
```

No intermediate states. Deletion is hard (not soft-delete) — the entity row is removed, and orphaned references in other modules are cleaned up asynchronously via domain event.

---

## Database Indexes

| Table | Index | Type |
|-------|-------|------|
| `custom_field_definitions` | `(tenant_id, name)` | Unique |
| `course_field_assignments` | `(course_id, field_definition_id)` | Unique |
| `participant_field_values` | `(registration_id, field_definition_id)` | Unique |
| `participant_field_values` | `(tenant_id)` | Non-unique (global query filter) |
