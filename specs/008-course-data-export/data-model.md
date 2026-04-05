# Data Model: Course Data Export

**Phase**: 1 — Design  
**Branch**: `008-course-data-export`  
**Date**: 2026-04-04

---

## No New Persistent Entities

This feature introduces **no new database tables, columns, or EF Core migrations**. All exported data is read from existing entities. The data model section documents the export-specific DTOs and the column definition model only.

---

## Export DTOs (Application Layer — read-only, never persisted)

### CourseExportRow

Represents one row in a courses-only export. Produced by `ExportCoursesQueryHandler`.

| Field | Source | Notes |
|-------|--------|-------|
| `Title` | `Course.Title` | |
| `Description` | `Course.Description` | May be empty |
| `CourseType` | `Course.CourseType` | Enum, exported as display string |
| `RegistrationMode` | `Course.RegistrationMode` | Enum, exported as display string |
| `Capacity` | `Course.Capacity` | |
| `EnrolledCount` | Computed: count of `Registration` with status ≠ Cancelled for this course | Cross-module; fetched by API orchestrator |
| `WaitlistedCount` | Count of Waitlisted registrations | Cross-module; fetched by API orchestrator |
| `Status` | `Course.Status` | Enum, exported as display string |
| `FirstSessionAt` | Earliest `Session.ScheduledAt` | Null if no sessions |
| `LastSessionEndsAt` | Latest `Session.EndsAt` | Null if no sessions |
| `Location` | `Session.Location` of first session | Representative location |

### ParticipantExportRow

Represents one row in a with-participants or roster export. One row per registration. Produced by `ExportCourseRosterQueryHandler`.

| Field | Source | Notes |
|-------|--------|-------|
| `CourseName` | `Course.Title` | Repeated per participant row |
| `CourseFirstSessionAt` | First session date | Repeated per participant row |
| `CourseLocation` | First session location | Repeated per participant row |
| `CourseStatus` | `Course.Status` | Repeated per participant row |
| `ParticipantName` | `Registration.ParticipantName` | |
| `ParticipantEmail` | `Registration.ParticipantEmail.Value` | |
| `EnrollmentStatus` | `Registration.Status` | Enum: Confirmed / Cancelled |
| `EnrollmentDate` | `Registration.RegisteredAt` | ISO 8601 |
| `[CustomField.Name]` | `ParticipantFieldValue.Value` for matching `FieldDefinitionId` | One column per enabled custom field; empty string if no value |
| `ExcusalCount` | Count of excusals for this registration | Present only when Excusals plugin is active for tenant |

### ExportColumnDefinition

Represents a selectable column in the export options UI. Served from `GET /api/v1/courses/export/columns`.

| Field | Type | Description |
|-------|------|-------------|
| `Key` | string | Machine-readable identifier (e.g., `course_title`, `participant_email`) |
| `LabelKey` | string | i18n translation key |
| `Group` | enum | `CourseInfo` \| `ParticipantInfo` \| `CustomFields` |
| `DefaultEnabled` | bool | Pre-checked in export options modal |
| `RequiresParticipants` | bool | True for participant columns; hidden when include-participants is off |

### ExportOptions (frontend session state — never sent to backend as-is)

| Field | Type | Description |
|-------|------|-------------|
| `includeParticipants` | bool | If true, flat participant rows are returned |
| `dateFrom` | Date \| null | Filter: course starts on or after this date |
| `dateTo` | Date \| null | Filter: course starts on or before this date |
| `statusFilter` | CourseStatus[] \| null | Filter: empty = all statuses |
| `selectedColumns` | string[] | Keys of columns to include |

---

## Column Groups

### Course Info Columns (always available)

| Key | Label | Default |
|-----|-------|---------|
| `course_title` | Course name | ✅ |
| `course_status` | Status | ✅ |
| `course_first_session_at` | Start date | ✅ |
| `course_last_session_ends_at` | End date | ✅ |
| `course_location` | Location | ✅ |
| `course_capacity` | Capacity | ✅ |
| `course_enrolled_count` | Enrolled | ✅ |
| `course_waitlisted_count` | Waitlisted | ✅ |
| `course_type` | Course type | ☐ |
| `course_registration_mode` | Registration mode | ☐ |
| `course_description` | Description | ☐ |

### Participant Info Columns (available only when include-participants = true)

| Key | Label | Default |
|-----|-------|---------|
| `participant_name` | Full name | ✅ |
| `participant_email` | Email | ✅ |
| `enrollment_status` | Enrollment status | ✅ |
| `enrollment_date` | Enrolled on | ✅ |
| `excusal_count` | Excusal count | ☐ (only shown if Excusals plugin active) |

### Custom Field Columns (dynamic — available only when include-participants = true)

Each active custom field definition for the tenant generates one entry:

| Key | Label | Default |
|-----|-------|---------|
| `cf_{fieldDefinitionId}` | Field name from definition | ☐ |

---

## Existing Entities Used (read-only)

| Entity | Module | Key Fields Used |
|--------|--------|----------------|
| `Course` | Courses | `Id`, `TenantId`, `Title`, `Description`, `CourseType`, `RegistrationMode`, `Capacity`, `Status` |
| `Session` | Courses | `ScheduledAt`, `EndsAt`, `Location` (first session) |
| `Registration` | Registrations | `Id`, `TenantId`, `CourseId`, `ParticipantName`, `ParticipantEmail`, `Status`, `RegisteredAt` |
| `ParticipantFieldValue` | Registrations | `RegistrationId`, `FieldDefinitionId`, `Value` |
| `CustomFieldDefinition` | Tenants | `Id`, `Name`, `FieldType`, `DisplayOrder` |
| `TenantPluginActivation` | Tenants | `PluginName` = "Excusals" check |

---

## Relationships (export assembly)

```
Tenant
  └── Courses (filtered by TenantId, date range, status)
        └── Sessions (first session for location/dates)
        └── Registrations (all, no pagination) [cross-module via MediatR]
              └── ParticipantFieldValues [per registration]
  └── CustomFieldDefinitions (active fields for tenant) [cross-module via MediatR]
  └── TenantPluginActivations (Excusals check) [cross-module via MediatR]
```

---

## Validation Rules

- At least one column key MUST be selected; export is rejected with 400 if `selectedColumns` is empty.
- `dateFrom` must be before `dateTo` if both are provided; API returns 400 otherwise.
- `courseId` in single-course roster export must belong to the requesting tenant; returns 404 if not found.
- Column keys in `selectedColumns` that do not exist in the column definition list are silently ignored (forward compatibility).
