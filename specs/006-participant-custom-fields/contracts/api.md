# API Contracts: Per-Participant Custom Field Values (006)

All endpoints require `Authorization: Bearer <token>` with `Staff` role unless noted.  
Tenant context is resolved from the authenticated request via `TenantResolutionMiddleware`.

---

## 1. Custom Field Definitions (Tenant Settings)

### List Field Definitions

```
GET /api/v1/settings/custom-fields
```

**Response 200**:
```json
[
  {
    "id": "3fa85f64-...",
    "name": "Deposit Paid",
    "fieldType": "YesNo",
    "allowedValues": [],
    "displayOrder": 1
  },
  {
    "id": "4cb96a75-...",
    "name": "Payment Plan",
    "fieldType": "OptionsList",
    "allowedValues": ["None", "Installment", "Full"],
    "displayOrder": 2
  }
]
```

---

### Create Field Definition

```
POST /api/v1/settings/custom-fields
```

**Request body**:
```json
{
  "name": "Deposit Paid",
  "fieldType": "YesNo",
  "allowedValues": []
}
```

**Field types**: `"YesNo"` | `"Text"` | `"OptionsList"`

**Validation**:
- `name`: required, max 100 chars, unique within tenant
- `allowedValues`: required non-empty array when `fieldType = "OptionsList"`; must be empty otherwise

**Response 201**:
```json
{
  "id": "3fa85f64-..."
}
```

**Response 422** (validation error):
```json
{
  "type": "validation_error",
  "errors": {
    "name": ["A field with this name already exists."]
  }
}
```

---

### Update Field Definition

```
PATCH /api/v1/settings/custom-fields/{fieldId}
```

**Request body** (all fields optional):
```json
{
  "name": "Deposit Received",
  "allowedValues": ["None", "Partial", "Full"]
}
```

**Notes**:
- `fieldType` cannot be changed after creation.
- Changing `allowedValues` does not affect existing `ParticipantFieldValue` records.

**Response 204** (no content)

**Response 404** if field not found in tenant.

---

### Delete Field Definition

```
DELETE /api/v1/settings/custom-fields/{fieldId}
```

**Response 204** (no content)

**Side effects** (async via domain event):
- Removes all `CourseFieldAssignment` records referencing this field.
- Existing `ParticipantFieldValue` records for this field are orphaned (values no longer surfaced in UI).

**Response 404** if field not found in tenant.

---

## 2. Course Custom Field Assignments

### Get Course Field Assignments

```
GET /api/v1/courses/{courseId}/custom-fields
```

Returns all tenant-level field definitions with assignment status for the course.

**Response 200**:
```json
[
  {
    "fieldDefinitionId": "3fa85f64-...",
    "name": "Deposit Paid",
    "fieldType": "YesNo",
    "allowedValues": [],
    "displayOrder": 1,
    "isEnabled": true
  },
  {
    "fieldDefinitionId": "4cb96a75-...",
    "name": "Notes",
    "fieldType": "Text",
    "allowedValues": [],
    "displayOrder": 2,
    "isEnabled": false
  }
]
```

---

### Update Course Field Assignments

```
PUT /api/v1/courses/{courseId}/custom-fields
```

Replaces the full set of enabled field assignments. Send only the `fieldDefinitionId` values of fields that should be enabled, in desired display order.

**Request body**:
```json
{
  "enabledFieldIds": [
    "3fa85f64-...",
    "4cb96a75-..."
  ]
}
```

**Notes**:
- Fields not in `enabledFieldIds` are disabled for this course.
- `DisplayOrder` is derived from the order of IDs in the array.
- All IDs must belong to the current tenant.

**Response 204** (no content)

**Response 422** if any `fieldDefinitionId` does not belong to the tenant.

---

## 3. Participant Field Values

### Roster Response Extension

`GET /api/v1/courses/{courseId}/registrations` response is extended to include `customFieldValues` per registration:

```json
{
  "items": [
    {
      "registrationId": "abc123-...",
      "participantName": "Jane Doe",
      "participantEmail": "jane@example.com",
      "registrationSource": "SelfService",
      "status": "Confirmed",
      "registeredAt": "2026-03-01T10:00:00Z",
      "customFieldValues": {
        "3fa85f64-...": "true",
        "4cb96a75-...": null
      }
    }
  ],
  "enabledCustomFields": [
    {
      "fieldDefinitionId": "3fa85f64-...",
      "name": "Deposit Paid",
      "fieldType": "YesNo",
      "allowedValues": [],
      "displayOrder": 1
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

**Notes**:
- `enabledCustomFields` provides column metadata so the frontend knows which columns to render and their display order.
- `customFieldValues` is a map of `fieldDefinitionId → serialized value string | null`.
- Existing roster consumers that ignore unknown fields are unaffected (backward-compatible).

---

### Set Participant Field Value

```
PATCH /api/v1/courses/{courseId}/registrations/{registrationId}/field-values
```

**Request body**:
```json
{
  "fieldDefinitionId": "3fa85f64-...",
  "value": "true"
}
```

**Value formats**:
- `YesNo`: `"true"` or `"false"` or `null` (clears)
- `Text`: any string or `null` (clears)
- `OptionsList`: one of the `allowedValues` strings or `null` (clears)

**Validation**:
- `fieldDefinitionId` must be enabled for the course.
- `value` must match the field type's allowed values when `fieldType = "OptionsList"`.

**Response 204** (no content)

**Response 404** if registration not found in tenant/course.

**Response 422** (validation error):
```json
{
  "type": "validation_error",
  "errors": {
    "value": ["'Banana' is not a valid option. Allowed: None, Partial, Full"]
  }
}
```

---

## Error Conventions

Follows the existing project error shape:

```json
{
  "type": "validation_error | not_found | forbidden",
  "title": "Human-readable title",
  "errors": { "fieldName": ["message"] }
}
```
