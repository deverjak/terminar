# API Contract: List Courses

**Endpoint**: `GET /api/v1/courses/`  
**Auth**: Bearer token (Staff or Admin)  
**Tenant**: Resolved from JWT claims via `ITenantContext`

## Current Response Shape

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Yoga Beginner",
    "description": "...",
    "courseType": "MultiSession",
    "registrationMode": "Open",
    "capacity": 20,
    "status": "Active",
    "sessionCount": 8,
    "firstSessionAt": "2026-04-10T09:00:00Z"
  }
]
```

## Updated Response Shape (this feature)

Two new fields added — fully backward-compatible (additive change):

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Yoga Beginner",
    "description": "...",
    "courseType": "MultiSession",
    "registrationMode": "Open",
    "capacity": 20,
    "status": "Active",
    "sessionCount": 8,
    "firstSessionAt": "2026-04-10T09:00:00Z",
    "lastSessionEndsAt": "2026-06-05T11:30:00Z",
    "tags": ["yoga", "beginner"]
  }
]
```

## Field Definitions

| Field              | Type              | Notes                                                          |
|--------------------|-------------------|----------------------------------------------------------------|
| `id`               | `guid`            | Course identifier                                              |
| `title`            | `string`          |                                                                |
| `description`      | `string`          |                                                                |
| `courseType`       | `"OneTime"` \| `"MultiSession"` |                                                |
| `registrationMode` | `"Open"` \| `"StaffOnly"` |                                                      |
| `capacity`         | `integer`         |                                                                |
| `status`           | `"Draft"` \| `"Active"` \| `"Cancelled"` \| `"Completed"` |       |
| `sessionCount`     | `integer`         |                                                                |
| `firstSessionAt`   | `datetime?`       | UTC ISO 8601; null if no sessions                              |
| `lastSessionEndsAt`| `datetime?`       | **NEW** UTC ISO 8601; null if no sessions                      |
| `tags`             | `string[]`        | **NEW** from `ExcusalPolicy.Tags`; empty array if none         |

## Backward Compatibility

The two new fields are **additive**. Existing consumers that do not read them are unaffected. No versioning change required.

## No New Query Parameters

Filtering, sorting, and pagination are performed client-side. No query parameters are added in this feature iteration.
