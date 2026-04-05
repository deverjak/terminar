# API Contract: Course Data Export Endpoints

**Feature**: `008-course-data-export`  
**Auth**: All endpoints require `StaffOrAdmin` authorization (JWT bearer, tenant resolved from token)

---

## GET /api/v1/courses/export/columns

Returns the list of available export columns for the current tenant. Custom field columns are dynamically generated based on the tenant's active field definitions.

### Response: 200 OK

```json
{
  "columns": [
    {
      "key": "course_title",
      "labelKey": "export.columns.course_title",
      "group": "CourseInfo",
      "defaultEnabled": true,
      "requiresParticipants": false
    },
    {
      "key": "participant_name",
      "labelKey": "export.columns.participant_name",
      "group": "ParticipantInfo",
      "defaultEnabled": true,
      "requiresParticipants": true
    },
    {
      "key": "excusal_count",
      "labelKey": "export.columns.excusal_count",
      "group": "ParticipantInfo",
      "defaultEnabled": false,
      "requiresParticipants": true
    },
    {
      "key": "cf_3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "labelKey": "export.columns.custom_field",
      "label": "Company",
      "group": "CustomFields",
      "defaultEnabled": false,
      "requiresParticipants": true
    }
  ]
}
```

**Notes**:
- `excusal_count` is only present in the response when the Excusals plugin is active for the tenant.
- Custom field entries include an additional `label` field with the field's display name (not an i18n key).
- `labelKey` for `CustomFields` group is a generic key; use `label` for display.

---

## GET /api/v1/courses/export

Exports courses (optionally with participants) as a CSV file download.

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `include_participants` | bool | No | Default: `false`. If `true`, one row per participant per course. If `false`, one row per course. |
| `date_from` | string (ISO 8601 date) | No | Filter: include only courses whose first session is on or after this date. |
| `date_to` | string (ISO 8601 date) | No | Filter: include only courses whose first session is on or before this date. |
| `status` | string | No | Filter: `Draft`, `Active`, `Cancelled`, `Completed`. Omit for all statuses. |
| `columns` | string[] (repeated param) | Yes | List of column keys to include. Must contain at least one key. Example: `?columns=course_title&columns=course_status` |

### Response: 200 OK

```
Content-Type: text/csv; charset=utf-8
Content-Disposition: attachment; filename="courses-export-2026-04-04.csv"
```

File body: UTF-8 with BOM, RFC 4180-compliant CSV.

**Courses-only example** (`include_participants=false`):
```csv
﻿Course name,Status,Start date,End date,Location,Capacity,Enrolled,Waitlisted
"Advanced Czech Grammar","Active","2026-04-10","2026-06-20","Room A1","20","18","3"
"Business English B2","Active","2026-04-15","2026-07-01","Online","30","12","0"
```

**With-participants example** (`include_participants=true`):
```csv
﻿Course name,Start date,Location,Full name,Email,Enrollment status,Enrolled on,Company
"Advanced Czech Grammar","2026-04-10","Room A1","Jana Nováková","jana@example.com","Confirmed","2026-03-15","Acme s.r.o."
"Advanced Czech Grammar","2026-04-10","Room A1","Petr Dvořák","petr@example.com","Confirmed","2026-03-17",""
```

### Error Responses

| Status | Condition |
|--------|-----------|
| 400 Bad Request | `columns` is empty or not provided |
| 400 Bad Request | `date_from` is after `date_to` |
| 200 OK (header-only CSV) | Filters matched zero courses — returns CSV with only header row |

---

## GET /api/v1/courses/{courseId}/registrations/export

Exports the participant roster for a single course as a CSV file download.

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `courseId` | GUID | ID of the course to export |

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `columns` | string[] (repeated param) | Yes | List of column keys from the participant/custom-field groups. Must contain at least one key. |

### Response: 200 OK

```
Content-Type: text/csv; charset=utf-8
Content-Disposition: attachment; filename="course-{courseId}-participants-2026-04-04.csv"
```

**Example**:
```csv
﻿Full name,Email,Enrollment status,Enrolled on,Dietary requirements
"Jana Nováková","jana@example.com","Confirmed","2026-03-15","Vegetarian"
"Petr Dvořák","petr@example.com","Confirmed","2026-03-17",""
```

### Error Responses

| Status | Condition |
|--------|-----------|
| 404 Not Found | `courseId` does not exist or does not belong to the tenant |
| 400 Bad Request | `columns` is empty or not provided |
| 200 OK (header-only CSV) | Course has no registrations — returns CSV with only header row |

---

## Column Keys Reference

### CourseInfo group

| Key | CSV Column Header (en) |
|-----|------------------------|
| `course_title` | Course name |
| `course_status` | Status |
| `course_first_session_at` | Start date |
| `course_last_session_ends_at` | End date |
| `course_location` | Location |
| `course_capacity` | Capacity |
| `course_enrolled_count` | Enrolled |
| `course_waitlisted_count` | Waitlisted |
| `course_type` | Course type |
| `course_registration_mode` | Registration mode |
| `course_description` | Description |

### ParticipantInfo group

| Key | CSV Column Header (en) |
|-----|------------------------|
| `participant_name` | Full name |
| `participant_email` | Email |
| `enrollment_status` | Enrollment status |
| `enrollment_date` | Enrolled on |
| `excusal_count` | Excusal count |

### CustomFields group (dynamic)

| Key pattern | CSV Column Header |
|-------------|------------------|
| `cf_{fieldDefinitionId}` | The field's `Name` from `CustomFieldDefinition` |
