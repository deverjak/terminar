# API Contracts: Course Reservation System — Feature 001

**Phase 1 Output** | Branch: `001-course-reservation-system` | Date: 2026-03-30

---

## General Conventions

- **Base URL**: `/api/v1`
- **Content-Type**: `application/json`
- **Auth**: `Authorization: Bearer <jwt>` for protected endpoints
- **Tenant resolution**:
  - Authenticated endpoints: tenant resolved from JWT claim `tenant_id`
  - Public endpoints: tenant resolved from `X-Tenant-Id: <slug-or-guid>` header
- **Timestamps**: ISO 8601 with timezone offset (e.g., `"2026-06-15T10:00:00+02:00"`)
- **IDs**: UUIDs (`"xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"`)

### Standard Error Response

```json
{
  "type": "https://terminar.app/errors/validation-failed",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "field_name": ["Error message"]
  }
}
```

HTTP status codes used:
- `200 OK` — successful read/update
- `201 Created` — successful creation (with `Location` header)
- `204 No Content` — successful delete/cancel
- `400 Bad Request` — malformed request body
- `401 Unauthorized` — missing or invalid JWT
- `403 Forbidden` — authenticated but insufficient role
- `404 Not Found` — resource not found (or not in caller's tenant)
- `409 Conflict` — duplicate registration, slug already taken
- `422 Unprocessable Entity` — validation failure

---

## Authentication Endpoints

### `POST /api/v1/auth/login`

Staff login. Returns JWT access token and refresh token.

**Access**: Public

**Request**:
```json
{
  "username": "staff@example.com",
  "password": "s3cur3P@ss"
}
```

**Response `200 OK`**:
```json
{
  "access_token": "<jwt>",
  "refresh_token": "<opaque-token>",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

**Errors**: `401` if credentials invalid or user deactivated.

---

### `POST /api/v1/auth/refresh`

Exchange a refresh token for a new access token.

**Access**: Public

**Request**:
```json
{
  "refresh_token": "<opaque-token>"
}
```

**Response `200 OK`**: Same shape as login response.

**Errors**: `401` if refresh token invalid or expired.

---

## Tenant Endpoints

### `POST /api/v1/tenants`

Create a new tenant. System-level operation.

**Access**: `SystemAdmin` role (special JWT claim)

**Request**:
```json
{
  "name": "Coding Academy Prague",
  "slug": "coding-academy-prague",
  "default_language_code": "cs",
  "admin_username": "admin",
  "admin_email": "admin@coding-academy.cz",
  "admin_password": "InitialP@ss123"
}
```

**Response `201 Created`**:
```json
{
  "tenant_id": "uuid",
  "name": "Coding Academy Prague",
  "slug": "coding-academy-prague",
  "status": "Active",
  "created_at": "2026-03-30T10:00:00+00:00"
}
```

**Location header**: `/api/v1/tenants/{tenant_id}`

**Errors**: `409` if slug already taken.

---

### `GET /api/v1/tenants/{tenantId}`

Get tenant details.

**Access**: `Admin` of that tenant, or `SystemAdmin`

**Response `200 OK`**:
```json
{
  "tenant_id": "uuid",
  "name": "Coding Academy Prague",
  "slug": "coding-academy-prague",
  "default_language_code": "cs",
  "status": "Active",
  "created_at": "2026-03-30T10:00:00+00:00"
}
```

---

## Course Endpoints

All course endpoints require `X-Tenant-Id` header (public) or resolve tenant from JWT (staff).

---

### `POST /api/v1/courses`

Create a new course.

**Access**: `Staff` or `Admin` role (authenticated)

**Request — one-time course**:
```json
{
  "title": "Introduction to .NET 10",
  "description": "A hands-on overview of .NET 10 features.",
  "course_type": "OneTime",
  "registration_mode": "Open",
  "capacity": 20,
  "sessions": [
    {
      "scheduled_at": "2026-06-15T09:00:00+02:00",
      "duration_minutes": 480,
      "location": "Room A, Building 1"
    }
  ]
}
```

**Request — multi-session course**:
```json
{
  "title": "Advanced C# Bootcamp",
  "description": "11-week intensive course.",
  "course_type": "MultiSession",
  "registration_mode": "StaffOnly",
  "capacity": 15,
  "sessions": [
    { "scheduled_at": "2026-09-01T09:00:00+02:00", "duration_minutes": 180, "location": "Room B" },
    { "scheduled_at": "2026-09-08T09:00:00+02:00", "duration_minutes": 180, "location": "Room B" }
  ]
}
```

**Response `201 Created`**:
```json
{
  "course_id": "uuid",
  "title": "Introduction to .NET 10",
  "course_type": "OneTime",
  "registration_mode": "Open",
  "capacity": 20,
  "status": "Active",
  "sessions": [
    {
      "session_id": "uuid",
      "scheduled_at": "2026-06-15T09:00:00+02:00",
      "duration_minutes": 480,
      "location": "Room A, Building 1",
      "sequence": 1
    }
  ],
  "created_at": "2026-03-30T10:00:00+00:00"
}
```

**Errors**: `422` if session count doesn't match `course_type`; `422` if sessions overlap.

---

### `GET /api/v1/courses`

List courses. Staff see all statuses; public (unauthenticated with `X-Tenant-Id`) see only `Active` + `Open` registration.

**Access**: Public (filtered) or Staff/Admin (full)

**Query parameters**:
- `status` — filter by `Draft|Active|Cancelled|Completed` (staff only)
- `page` — page number, default 1
- `page_size` — items per page, default 20, max 100

**Response `200 OK`**:
```json
{
  "items": [
    {
      "course_id": "uuid",
      "title": "Introduction to .NET 10",
      "course_type": "OneTime",
      "registration_mode": "Open",
      "capacity": 20,
      "confirmed_registrations": 7,
      "status": "Active",
      "next_session_at": "2026-06-15T09:00:00+02:00"
    }
  ],
  "total": 42,
  "page": 1,
  "page_size": 20
}
```

---

### `GET /api/v1/courses/{courseId}`

Get full course details including all sessions.

**Access**: Public (Active Open courses) or Staff/Admin (all)

**Response `200 OK`**: Same as POST 201 response, with `confirmed_registrations` count added.

---

### `PUT /api/v1/courses/{courseId}`

Update course details. Only allowed for `Draft` or `Active` courses. Sessions cannot be removed if registrations exist.

**Access**: `Staff` or `Admin` role

**Request**: Same shape as POST, all fields optional (partial update).

**Response `200 OK`**: Updated course object.

**Errors**: `409` if course is `Cancelled` or `Completed`.

---

### `POST /api/v1/courses/{courseId}/cancel`

Cancel a course. Marks it as Cancelled; does not delete registrations.

**Access**: `Admin` role only

**Request**: (empty body or optional `{ "reason": "..." }`)

**Response `204 No Content`**

**Errors**: `409` if course is already `Cancelled` or `Completed`.

---

## Registration Endpoints

---

### `POST /api/v1/courses/{courseId}/registrations`

Register a participant for a course. Works for both self-service (public) and staff-added.

**Access**:
- Public (`X-Tenant-Id` header): course MUST have `registration_mode = Open`
- Staff/Admin (JWT): any course regardless of registration mode

**Request**:
```json
{
  "participant_name": "Jan Novák",
  "participant_email": "jan.novak@example.com"
}
```

**Response `201 Created`**:
```json
{
  "registration_id": "uuid",
  "course_id": "uuid",
  "participant_name": "Jan Novák",
  "participant_email": "jan.novak@example.com",
  "registration_source": "SelfService",
  "status": "Confirmed",
  "registered_at": "2026-03-30T10:00:00+00:00"
}
```

**Location header**: `/api/v1/courses/{courseId}/registrations/{registration_id}`

**Errors**:
- `409` — participant already registered for this course
- `422` — course is full (capacity reached)
- `422` — course is `Cancelled` or `Completed`
- `403` — course is `StaffOnly` and caller is not authenticated staff

---

### `GET /api/v1/courses/{courseId}/registrations`

Get the full participant roster for a course.

**Access**: `Staff` or `Admin` role only

**Query parameters**:
- `status` — filter by `Confirmed|Cancelled`, default `Confirmed`
- `page`, `page_size` — pagination

**Response `200 OK`**:
```json
{
  "items": [
    {
      "registration_id": "uuid",
      "participant_name": "Jan Novák",
      "participant_email": "jan.novak@example.com",
      "registration_source": "SelfService",
      "status": "Confirmed",
      "registered_at": "2026-03-30T10:00:00+00:00"
    }
  ],
  "total": 7,
  "page": 1,
  "page_size": 20
}
```

---

### `DELETE /api/v1/courses/{courseId}/registrations/{registrationId}`

Cancel a registration.

**Access**:
- Self-cancellation: requires a cancellation token (emailed to participant at registration) — `?token=<cancellation-token>`
- Staff cancellation: `Staff` or `Admin` JWT

**Response `204 No Content`**

**Errors**:
- `403` — caller is not staff and token is missing/invalid
- `409` — registration is already `Cancelled`
- `422` — all course sessions have already passed (cancellation window closed)

---

## Staff User Endpoints

### `POST /api/v1/staff`

Create a new staff user for the caller's tenant.

**Access**: `Admin` role

**Request**:
```json
{
  "username": "jiri.novotny",
  "email": "jiri@coding-academy.cz",
  "password": "InitialP@ss123",
  "role": "Staff"
}
```

**Response `201 Created`**:
```json
{
  "staff_user_id": "uuid",
  "username": "jiri.novotny",
  "email": "jiri@coding-academy.cz",
  "role": "Staff",
  "status": "Active",
  "created_at": "2026-03-30T10:00:00+00:00"
}
```

**Errors**: `409` if username or email already exists in this tenant.

---

### `GET /api/v1/staff`

List staff users for the caller's tenant.

**Access**: `Admin` role

**Response `200 OK`**: Paginated list of staff user objects (no password hash).

---

### `DELETE /api/v1/staff/{staffUserId}`

Deactivate a staff user (soft delete).

**Access**: `Admin` role

**Response `204 No Content`**

---

## JWT Claims Reference

| Claim | Value | Description |
|-------|-------|-------------|
| `sub` | StaffUserId (UUID) | Subject — staff user ID |
| `tenant_id` | TenantId (UUID) | Tenant this user belongs to |
| `role` | `Admin` \| `Staff` \| `SystemAdmin` | Authorization role |
| `exp` | Unix timestamp | Expiry (1 hour default) |
| `jti` | UUID | JWT ID (for refresh token revocation) |
