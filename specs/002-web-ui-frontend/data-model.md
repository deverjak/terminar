# Data Model: Web UI Frontend

**Branch**: `002-web-ui-frontend` | **Date**: 2026-03-31

This document describes the **frontend data model** — the TypeScript types and shapes the frontend works with, derived directly from the backend API contracts.

---

## Auth & Session State

```typescript
// Stored in AuthContext (in-memory + localStorage for refresh token)
interface AuthSession {
  accessToken: string;          // JWT, in-memory only
  refreshToken: string;         // stored in localStorage
  userId: string;               // UUID, from JWT sub claim
  username: string;
  role: 'Admin' | 'Staff';
  tenantSlug: string;           // stored alongside tokens for API headers
  tenantId: string;             // UUID, from JWT claims
}
```

---

## Tenant

```typescript
// POST /api/v1/tenants request
interface CreateTenantRequest {
  name: string;                 // required
  slug: string;                 // required, unique, URL-safe
  defaultLanguageCode: string;  // e.g. "en", "cs"
  adminUsername: string;
  adminEmail: string;
  adminPassword: string;        // user-chosen during onboarding
}

// POST /api/v1/tenants response
interface CreateTenantResponse {
  tenant_id: string;
  name: string;
  slug: string;
  status: string;
  created_at: string;           // ISO 8601 UTC datetime
}
```

---

## Auth (Login / Refresh)

```typescript
// POST /api/v1/auth/login request
interface LoginRequest {
  username: string;
  password: string;
  // tenant slug passed as X-Tenant-Slug header
}

// POST /api/v1/auth/login response (same shape for refresh)
interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;            // seconds
  tokenType: string;            // "Bearer"
}

// POST /api/v1/auth/refresh request
interface RefreshTokenRequest {
  userId: string;
  refreshToken: string;
}
```

---

## Staff Users

```typescript
interface StaffUser {
  staffUserId: string;
  username: string;
  email: string;
  role: 'Admin' | 'Staff';
  status: 'Active' | 'Inactive';
  createdAt: string;            // ISO 8601 UTC datetime
}

// POST /api/v1/staff request
interface CreateStaffUserRequest {
  username: string;
  email: string;
  password: string;
  role: 'Admin' | 'Staff';
}
```

---

## Courses

```typescript
type CourseType = 'OneTime' | 'MultiSession';
type RegistrationMode = 'Open' | 'StaffOnly';
type CourseStatus = 'Draft' | 'Active' | 'Cancelled' | 'Completed';

// List item (GET /api/v1/courses)
interface CourseListItem {
  id: string;
  title: string;
  description: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: int;
  status: CourseStatus;
  sessionCount: number;
  firstSessionAt: string | null;  // ISO 8601 UTC datetime
}

// Full detail (GET /api/v1/courses/:id)
interface CourseDetail {
  id: string;
  title: string;
  description: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: number;
  status: CourseStatus;
  createdByStaffId: string;
  createdAt: string;
  updatedAt: string;
  sessions: SessionDetail[];
}

interface SessionDetail {
  id: string;
  sequence: number;
  scheduledAt: string;          // ISO 8601 UTC datetime
  durationMinutes: number;
  location: string | null;
  endsAt: string;               // ISO 8601 UTC datetime
}

// POST /api/v1/courses request
interface CreateCourseRequest {
  title: string;
  description?: string;
  courseType: CourseType;
  registrationMode: RegistrationMode;
  capacity: number;
  sessions: SessionInput[];
}

interface SessionInput {
  scheduledAt: string;          // ISO 8601 UTC datetime, must be future
  durationMinutes: number;
  location?: string;
}

// PATCH /api/v1/courses/:id request
interface UpdateCourseRequest {
  title?: string;
  description?: string;
  capacity?: number;
  registrationMode?: RegistrationMode;
}
```

---

## Registrations

```typescript
type RegistrationSource = 'SelfService' | 'StaffAdded';
type RegistrationStatus = 'Confirmed' | 'Cancelled';

interface Registration {
  registrationId: string;
  participantName: string;
  participantEmail: string;
  registrationSource: RegistrationSource;
  status: RegistrationStatus;
  registeredAt: string;         // ISO 8601 UTC datetime
}

// Full registration (returned on create)
interface RegistrationCreated extends Registration {
  courseId: string;
  cancellationToken: string;    // UUID, for self-service cancellation
}

// POST /api/v1/courses/:courseId/registrations request
interface CreateRegistrationRequest {
  participantName: string;
  participantEmail: string;
}

// GET /api/v1/courses/:courseId/registrations response
interface RosterPage {
  items: Registration[];
  total: number;
  page: number;
  pageSize: number;
}
```

---

## UI-Only State Types

These types exist only in the frontend and are not persisted to the backend.

```typescript
// Calendar event derived from sessions
interface CalendarEvent {
  id: string;               // session ID
  courseId: string;
  courseTitle: string;
  date: Date;               // local date from scheduledAt
  durationMinutes: number;
  location: string | null;
}

// Pagination state (shared across list views)
interface PaginationState {
  page: number;
  pageSize: number;
  total: number;
}
```

---

## Validation Rules (Frontend)

| Field | Rule |
|-------|------|
| Tenant slug | Lowercase letters, digits, hyphens only; 3–63 chars |
| Admin password | Min 8 characters |
| Course title | Required, max 200 chars |
| Course capacity | Integer, min 1 |
| Session scheduledAt | Must be a future UTC datetime |
| Session durationMinutes | Integer, min 1 |
| Participant email | Valid email format |
| Participant name | Required, max 200 chars |
