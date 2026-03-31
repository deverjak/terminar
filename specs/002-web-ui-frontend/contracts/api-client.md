# API Client Contract: Frontend ↔ Backend

**Branch**: `002-web-ui-frontend` | **Date**: 2026-03-31

This document defines how the frontend communicates with the backend REST API. All endpoints are documented with headers, authentication requirements, and expected response shapes.

---

## Base URL

```
Development: http://localhost:5000
Production:  [configured via VITE_API_BASE_URL env var]
```

---

## Common Headers

| Header | Value | When Required |
|--------|-------|--------------|
| `Content-Type` | `application/json` | All POST/PATCH requests |
| `Authorization` | `Bearer {accessToken}` | All authenticated endpoints |
| `X-Tenant-Slug` | `{tenantSlug}` | All authenticated endpoints; also login |

---

## Authentication Flow

### 1. Create Tenant (Unauthenticated)
```
POST /api/v1/tenants
Headers: Content-Type: application/json
Body: { name, slug, defaultLanguageCode, adminUsername, adminEmail, adminPassword }
Response 201: { tenant_id, name, slug, status, created_at }
Response 422: { errors: { field: [message] } }
```
After successful creation, immediately call Login with the provided credentials.

### 2. Login
```
POST /api/v1/auth/login
Headers: Content-Type: application/json, X-Tenant-Slug: {slug}
Body: { username, password }
Response 200: { accessToken, refreshToken, expiresIn, tokenType }
Response 401: { error: "Invalid credentials" }
Response 403: { error: "Account is inactive" }
```

### 3. Refresh Token
```
POST /api/v1/auth/refresh
Headers: Content-Type: application/json
Body: { userId, refreshToken }
Response 200: { accessToken, refreshToken, expiresIn, tokenType }
Response 401: → clear tokens, redirect to /login
```

---

## Tenants

### Get Tenant (System Admin Only)
```
GET /api/v1/tenants/{id}
Headers: Authorization, X-Tenant-Slug
Response 200: { tenantId, name, slug, defaultLanguageCode, status, createdAt }
Response 403: (not system admin)
```

---

## Identity / Staff

### List Staff Users (Admin Only)
```
GET /api/v1/staff/
Headers: Authorization, X-Tenant-Slug
Response 200: StaffUser[]
  [{ staffUserId, username, email, role, status, createdAt }]
```

### Create Staff User (Admin Only)
```
POST /api/v1/staff/
Headers: Authorization, X-Tenant-Slug, Content-Type
Body: { username, email, password, role }
Response 201: { staffUserId, username, email, role, createdAt }
Response 422: validation errors
```

### Deactivate Staff User (Admin Only)
```
POST /api/v1/staff/{id}/deactivate
Headers: Authorization, X-Tenant-Slug
Response 204: (no content)
Response 404: (not found or not in tenant)
```

---

## Courses

### List Courses
```
GET /api/v1/courses/
Headers: Authorization, X-Tenant-Slug
Response 200: CourseListItem[]
  [{ id, title, description, courseType, registrationMode, capacity, status, sessionCount, firstSessionAt }]
```

### Get Course Detail
```
GET /api/v1/courses/{id}
Headers: Authorization, X-Tenant-Slug
Response 200: CourseDetail
  { id, title, description, courseType, registrationMode, capacity, status,
    createdByStaffId, createdAt, updatedAt,
    sessions: [{ id, sequence, scheduledAt, durationMinutes, location, endsAt }] }
Response 404: (not found or not in tenant)
```

### Create Course
```
POST /api/v1/courses/
Headers: Authorization, X-Tenant-Slug, Content-Type
Body: { title, description?, courseType, registrationMode, capacity, sessions: [{ scheduledAt, durationMinutes, location? }] }
Response 201: { id }
Response 422: validation errors (e.g. past session date)
```

### Update Course
```
PATCH /api/v1/courses/{id}
Headers: Authorization, X-Tenant-Slug, Content-Type
Body: { title?, description?, capacity?, registrationMode? }
Response 204: (no content)
Response 404: (not found or not in tenant)
```

### Cancel Course
```
POST /api/v1/courses/{id}/cancel
Headers: Authorization, X-Tenant-Slug
Response 204: (no content)
Response 404: (not found or not in tenant)
Response 422: (already cancelled or completed)
```

---

## Registrations

### Get Course Roster
```
GET /api/v1/courses/{courseId}/registrations?page=1&pageSize=20&statusFilter=Confirmed
Headers: Authorization, X-Tenant-Slug
Response 200: { items: Registration[], total, page, pageSize }
  Registration: { registrationId, participantName, participantEmail, registrationSource, status, registeredAt }
```

### Create Registration
```
POST /api/v1/courses/{courseId}/registrations
Headers: Authorization, X-Tenant-Slug, Content-Type
Body: { participantName, participantEmail }
Response 201: { registrationId, courseId, participantName, participantEmail,
                registrationSource, status, registeredAt, cancellationToken }
Response 409: (course at full capacity)
Response 422: validation errors
```

### Cancel Registration
```
DELETE /api/v1/courses/{courseId}/registrations/{registrationId}
Headers: Authorization, X-Tenant-Slug
Response 204: (no content)
Response 404: (not found)
```

---

## Error Handling Contract

| HTTP Status | Frontend Behavior |
|-------------|------------------|
| 400 | Show generic "Bad request" notification |
| 401 | Attempt token refresh; if fails → redirect to /login |
| 403 | Show "Not authorized" notification; do not redirect |
| 404 | Show "Not found" notification |
| 409 | Show conflict message (e.g., "Course is full") |
| 422 | Map field errors to form field inline errors |
| 5xx | Show "Server error, please try again" notification |
| Network error | Show "Connection error" notification |

---

## Route Map (Frontend SPA)

| Path | Component | Auth |
|------|-----------|------|
| `/` | Landing page | No |
| `/register` | Tenant creation form | No |
| `/login` | Login form | No |
| `/app/courses` | Course list + calendar toggle | Yes |
| `/app/courses/new` | Create course form | Yes |
| `/app/courses/:id` | Course detail with sessions | Yes |
| `/app/courses/:id/edit` | Edit course form | Yes |
| `/app/courses/:courseId/registrations` | Course roster | Yes |
| `/app/staff` | Staff user list | Yes (Admin) |
| `/app/staff/new` | Create staff user form | Yes (Admin) |
