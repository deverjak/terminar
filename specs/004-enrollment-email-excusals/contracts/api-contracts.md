# API Contracts: Course Enrollment Email and Session Excusals

**Feature**: `004-enrollment-email-excusals`  
**Date**: 2026-04-02

All existing staff API endpoints are unchanged. New endpoints only.

---

## Public Participant Endpoints

No authentication required. Tenant resolved via `X-Tenant-Id` header (UUID or slug).

---

### POST /api/v1/participants/magic-link

Sends a magic link email to the participant if they have active enrollments in the tenant.  
Always returns `202 Accepted` regardless of whether the email exists (prevents email enumeration).

**Request**
```json
{ "email": "participant@example.com" }
```

**Response**
```
202 Accepted
(no body)
```

---

### POST /api/v1/participants/portal/redeem

Exchanges a single-use magic link token for a portal session token.

**Request**
```json
{ "token": "<magicLinkToken>" }
```

**Response `200 OK`**
```json
{
  "portalToken": "<32-byte-base64url>",
  "expiresAt": "2026-04-09T12:00:00Z"
}
```

**Errors**
- `422` — token not found, already used, or expired (generic message, no specifics)

---

### GET /api/v1/participants/portal

Returns all active enrollments and excusal credits for the participant.  
Auth: `X-Portal-Token: {portalToken}` header.

**Response `200 OK`**
```json
{
  "participantEmail": "participant@example.com",
  "participantName": "Jane Doe",
  "enrollments": [
    {
      "enrollmentId": "uuid",
      "safeLinkToken": "uuid",
      "courseId": "uuid",
      "courseTitle": "Yoga Q1",
      "status": "Confirmed",
      "firstSessionAt": "2026-01-15T09:00:00Z",
      "unenrollmentDeadlineAt": "2026-01-01T09:00:00Z",
      "canUnenroll": true
    }
  ],
  "excusalCredits": [
    {
      "creditId": "uuid",
      "sourceCourseTitle": "Yoga Q1",
      "sourceSessionAt": "2026-01-15T09:00:00Z",
      "tags": ["yoga", "beginner"],
      "validUntil": "2026-09-30",
      "status": "Active"
    }
  ]
}
```

**Errors**
- `401` — portal token missing, invalid, or expired

---

### GET /api/v1/participants/courses/{safeLinkToken}

Returns the participant's view of a specific course enrollment.  
No auth header required — the `safeLinkToken` (path param) is the credential.

**Response `200 OK`**
```json
{
  "enrollmentId": "uuid",
  "courseId": "uuid",
  "courseTitle": "Yoga Q1",
  "courseStatus": "Active",
  "participantName": "Jane Doe",
  "enrollmentStatus": "Confirmed",
  "unenrollmentDeadlineAt": "2026-01-01T09:00:00Z",
  "canUnenroll": true,
  "sessions": [
    {
      "sessionId": "uuid",
      "scheduledAt": "2026-01-15T09:00:00Z",
      "durationMinutes": 60,
      "location": "Studio A",
      "isPast": false,
      "excusalDeadlineAt": "2026-01-14T09:00:00Z",
      "excusalStatus": null
    }
  ],
  "excusalCredits": [
    {
      "creditId": "uuid",
      "tags": ["yoga"],
      "validUntil": "2026-09-30",
      "status": "Active"
    }
  ]
}
```

`excusalStatus` per session: `null | "Excused" | "CreditIssued"`

**Errors**
- `404` — token not found or enrollment cancelled

---

### POST /api/v1/participants/courses/{safeLinkToken}/unenroll

Unenrolls the participant from the entire course.

**Request**: no body

**Response `200 OK`**
```json
{ "message": "Successfully unenrolled." }
```

**Errors**
- `404` — token not found
- `422` — already unenrolled, or unenrollment deadline has passed

---

### POST /api/v1/participants/courses/{safeLinkToken}/sessions/{sessionId}/excuse

Excuses the participant from a specific session.

**Request**: no body

**Response `200 OK`**
```json
{
  "excusalId": "uuid",
  "excusalCreditId": "uuid | null",
  "creditGenerated": true
}
```

`creditGenerated: false` when the course has credit generation disabled.

**Errors**
- `404` — token or session not found
- `409` — already excused for this session
- `422` — excusal deadline has passed, or session is in the past

---

### POST /api/v1/participants/credits/{creditId}/redeem

Redeems an excusal credit for a makeup enrollment in a target course.  
Auth: `X-Portal-Token: {portalToken}` header.

**Request**
```json
{ "targetCourseId": "uuid" }
```

**Response `200 OK`**
```json
{ "newEnrollmentId": "uuid", "safeLinkToken": "uuid" }
```

**Errors**
- `401` — portal token missing/invalid/expired, or credit does not belong to the portal token's participant
- `404` — credit not found
- `409` — credit already redeemed or cancelled
- `422` — credit expired, target course has no matching tags, target course at capacity

---

## Staff Endpoints (authenticated, tenant-scoped)

All require `Authorization: Bearer {accessToken}` and tenant resolved from JWT claim.

---

### GET /api/v1/excusal-credits

Lists excusal credits for the tenant. Requires `StaffOrAdmin` policy.

**Query params**: `page` (default 1), `pageSize` (default 20), `status` (optional: `Active|Redeemed|Expired|Cancelled`), `participantEmail` (optional filter)

**Response `200 OK`**
```json
{
  "items": [
    {
      "creditId": "uuid",
      "participantEmail": "...",
      "participantName": "...",
      "sourceCourseTitle": "...",
      "sourceSessionAt": "2026-01-15T09:00:00Z",
      "tags": ["yoga"],
      "validUntil": "2026-09-30",
      "status": "Active",
      "createdAt": "...",
      "deletedAt": null,
      "auditEntries": []
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 20
}
```

---

### PATCH /api/v1/excusal-credits/{id}

Extends validity windows or replaces tags on an active credit. Requires `StaffOrAdmin` policy.

**Request** (all fields optional; at least one required)
```json
{
  "additionalWindowIds": ["uuid", "uuid"],
  "tags": ["yoga", "advanced"]
}
```

- `additionalWindowIds`: appended to existing window list (must reference valid windows, must come after current last window).
- `tags`: fully replaces existing tag set. Must not be empty.

**Response `200 OK`**
```json
{ "creditId": "uuid", "status": "Active" }
```

**Errors**
- `404` — credit not found
- `422` — credit not active, empty tags, window not found, window out of order

---

### DELETE /api/v1/excusal-credits/{id}

Soft-deletes an active credit. Permanent — no restore. Requires `StaffOrAdmin` policy.

**Response `204 No Content`**

**Errors**
- `404` — credit not found
- `422` — credit not active (already redeemed, expired, or cancelled)

---

### GET /api/v1/settings/excusal-windows

Lists all validity windows for the tenant. Requires `StaffOrAdmin` policy.

**Response `200 OK`**
```json
[
  {
    "windowId": "uuid",
    "name": "Q1/2026",
    "startDate": "2026-01-01",
    "endDate": "2026-03-31"
  }
]
```

---

### POST /api/v1/settings/excusal-windows

Creates a new validity window. Requires `AdminOnly` policy.

**Request**
```json
{ "name": "Q2/2026", "startDate": "2026-04-01", "endDate": "2026-06-30" }
```

**Response `201 Created`**
```json
{ "windowId": "uuid" }
```

**Errors**
- `409` — window name already exists for tenant
- `422` — endDate not after startDate

---

### PATCH /api/v1/settings/excusal-windows/{id}

Updates a validity window. Requires `AdminOnly` policy.

**Request** (all optional)
```json
{ "name": "Q2/2026 Updated", "startDate": "2026-04-01", "endDate": "2026-06-30" }
```

**Response `200 OK`**

**Errors**
- `404` — window not found
- `409` — name conflict
- `422` — date validation

---

### DELETE /api/v1/settings/excusal-windows/{id}

Soft-deletes a validity window. Requires `AdminOnly` policy.

**Response `204 No Content`**

**Errors**
- `404` — window not found
- `409` — window is referenced by one or more active excusal credits

---

### GET /api/v1/settings/excusal-policy

Returns tenant-wide excusal settings. Requires `StaffOrAdmin` policy.

**Response `200 OK`**
```json
{
  "creditGenerationEnabled": false,
  "forwardWindowCount": 2,
  "unenrollmentDeadlineDays": 14,
  "excusalDeadlineHours": 24
}
```

---

### PATCH /api/v1/settings/excusal-policy

Updates tenant-wide excusal settings. Requires `AdminOnly` policy.

**Request** (all optional)
```json
{
  "creditGenerationEnabled": true,
  "forwardWindowCount": 3,
  "unenrollmentDeadlineDays": 7,
  "excusalDeadlineHours": 48
}
```

**Response `200 OK`**

---

### GET /api/v1/courses/{courseId}/excusal-policy

Returns the excusal policy for a specific course. Requires `StaffOrAdmin` policy.

**Response `200 OK`**
```json
{
  "courseId": "uuid",
  "creditGenerationOverride": null,
  "validityWindowId": "uuid | null",
  "tags": ["yoga", "beginner"],
  "effectiveCreditGenerationEnabled": false
}
```

---

### PATCH /api/v1/courses/{courseId}/excusal-policy

Updates the excusal policy for a specific course. Requires `StaffOrAdmin` policy.

**Request** (all optional)
```json
{
  "creditGenerationOverride": true,
  "validityWindowId": "uuid",
  "tags": ["yoga", "beginner"]
}
```

Pass `"creditGenerationOverride": null` to clear the override (use tenant default).  
Pass `"validityWindowId": null` to unset the assigned window.

**Response `200 OK`**

**Errors**
- `422` — validityWindowId not found in tenant

---

## Frontend Routes (new)

| Route | Component | Auth |
|---|---|---|
| `/participant` | `ParticipantPortalRequestPage` | None (public) |
| `/participant/portal` | `ParticipantPortalPage` (reads `portalToken` from localStorage) | Portal token |
| `/participant/course/:safeLinkToken` | `ParticipantCourseViewPage` | None (token in URL) |
| `/app/settings/excusal` | `ExcusalSettingsPage` | Staff (Admin) |
| `/app/courses/:courseId/excusal-policy` | `CourseExcusalPolicyPage` | Staff |
| `/app/excusal-credits` | `ExcusalCreditsPage` | Staff |

---

## Email Types

| Email | Trigger | Recipients |
|---|---|---|
| `EnrollmentConfirmation` | `RegistrationCreated` domain event | Participant |
| `MagicLinkRequest` | `POST /participants/magic-link` command | Participant |
| `ExcusalConfirmation` | `ExcusalCreated` domain event | Participant |
| `UnenrollmentConfirmation` | `RegistrationCancelled` via safe link | Participant |
| `StaffUnenrollmentNotification` | `RegistrationCancelled` via safe link | Course organizer / tenant admin |
| `ExcusalCreditRedemptionConfirmation` | `ExcusalCreditRedeemed` domain event | Participant |

---

## SMTP Configuration (`appsettings.json`)

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "noreply@example.com",
  "Password": "<from environment or secrets>",
  "FromAddress": "noreply@example.com",
  "FromName": "Termínář",
  "UseSsl": false,
  "UseStartTls": true
}
```

Loaded via `builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"))`.
