# Data Model: Course Reservation System — Feature 001

**Phase 1 Output** | Branch: `001-course-reservation-system` | Date: 2026-03-30

---

## Bounded Contexts & Aggregate Roots

Four bounded contexts map to four modules. Each module owns its own PostgreSQL schema.

---

## Module: Tenants (`tenants` schema)

### Aggregate Root: `Tenant`

Represents an independent organization using the system.

| Field | Type | Constraints |
|-------|------|-------------|
| `TenantId` | `TenantId` (typed Guid) | PK, non-null |
| `Name` | `string` | Non-empty, max 200 chars |
| `Slug` | `string` | Non-empty, max 100 chars, unique, URL-safe |
| `DefaultLanguageCode` | `string` | Non-empty, 2–5 chars (BCP47, e.g., `"cs"`, `"en"`) |
| `Status` | `TenantStatus` | Enum: `Active`, `Suspended` |
| `CreatedAt` | `DateTimeOffset` | Non-null, set on creation |

**Invariants**:
- A Tenant MUST have at least one active StaffUser (enforced at application layer)
- Slug MUST be unique across the system (global uniqueness, not per-tenant)
- Status transition: `Active` → `Suspended` → `Active` (no deletion)

**Domain Events**:
- `TenantCreated`
- `TenantSuspended`
- `TenantReactivated`

---

## Module: Identity (`identity` schema)

### Aggregate Root: `StaffUser`

A human user with management permissions for a specific tenant.

| Field | Type | Constraints |
|-------|------|-------------|
| `StaffUserId` | `StaffUserId` (typed Guid) | PK, non-null |
| `TenantId` | `TenantId` | Non-null, FK to Tenant (cross-module reference by ID only) |
| `Username` | `string` | Non-empty, max 100 chars, unique within tenant |
| `Email` | `Email` (value object) | Valid email format, unique within tenant |
| `PasswordHash` | `string` | BCrypt hash, never stored in plain text |
| `Role` | `StaffRole` | Enum: `Admin`, `Staff` |
| `Status` | `StaffUserStatus` | Enum: `Active`, `Deactivated` |
| `CreatedAt` | `DateTimeOffset` | Non-null |
| `LastLoginAt` | `DateTimeOffset?` | Nullable |

**Value Objects**:
- `Email`: wraps string, validates format on construction, case-insensitive equality

**Invariants**:
- Username and Email MUST be unique per tenant
- Password MUST meet minimum complexity (min 8 chars, validated at application layer)
- A deactivated StaffUser MUST NOT be able to authenticate

**Domain Events**:
- `StaffUserCreated`
- `StaffUserDeactivated`

### Infrastructure: `AppIdentityUser` (Infrastructure layer only — not a domain type)

ASP.NET Core Identity's `IdentityUser` subclass. Lives exclusively in `Infrastructure/Identity/`. Invisible to the domain.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` (Guid) | ASP.NET Identity PK |
| `TenantId` | `Guid` | Additional field — maps to domain `TenantId` |
| `Role` | `string` | Maps to domain `StaffRole` |
| Inherited: `UserName`, `Email`, `PasswordHash`, `AccessFailedCount`, `LockoutEnd`, etc. | | Managed by ASP.NET Identity |

Refresh tokens are stored via `UserManager.SetAuthenticationTokenAsync` (ASP.NET Identity token store) — no custom `RefreshToken` entity needed.

---

## Module: Courses (`courses` schema)

### Aggregate Root: `Course`

A learning or training event, either single-session or multi-session.

| Field | Type | Constraints |
|-------|------|-------------|
| `CourseId` | `CourseId` (typed Guid) | PK, non-null |
| `TenantId` | `TenantId` | Non-null (global query filter applied) |
| `Title` | `string` | Non-empty, max 200 chars |
| `Description` | `string` | Max 5000 chars, may be empty |
| `CourseType` | `CourseType` | Enum: `OneTime`, `MultiSession` |
| `RegistrationMode` | `RegistrationMode` | Enum: `Open`, `StaffOnly` |
| `Capacity` | `int` | Min 1, max 10000 |
| `Status` | `CourseStatus` | Enum: `Draft`, `Active`, `Cancelled`, `Completed` |
| `CreatedByStaffId` | `StaffUserId` | Non-null (cross-module ref by ID only) |
| `CreatedAt` | `DateTimeOffset` | Non-null |
| `UpdatedAt` | `DateTimeOffset` | Non-null |

**Child Entity: `Session`** (belongs to Course aggregate)

| Field | Type | Constraints |
|-------|------|-------------|
| `SessionId` | `SessionId` (typed Guid) | PK within aggregate |
| `ScheduledAt` | `DateTimeOffset` | Non-null, must be in the future at creation |
| `DurationMinutes` | `int` | Min 1 |
| `Location` | `string?` | Optional, max 500 chars |
| `Sequence` | `int` | 1-based ordering within course |

**Invariants**:
- A `OneTime` course MUST have exactly 1 session
- A `MultiSession` course MUST have at least 2 sessions
- Sessions MUST NOT overlap in time (within the same course)
- A `Cancelled` or `Completed` course MUST NOT be editable
- Course MUST NOT transition from `Completed` or `Cancelled` to `Active`

**Status Transitions**:
```
Draft → Active → Cancelled
Draft → Active → Completed
Draft → Cancelled
```

**Domain Events**:
- `CourseCreated`
- `CourseActivated`
- `CourseCancelled`
- `CourseCapacityUpdated`

---

## Module: Registrations (`registrations` schema)

### Aggregate Root: `Registration`

Links a participant to a course. One registration = one person for one course.

| Field | Type | Constraints |
|-------|------|-------------|
| `RegistrationId` | `RegistrationId` (typed Guid) | PK, non-null |
| `TenantId` | `TenantId` | Non-null (global query filter applied) |
| `CourseId` | `CourseId` | Non-null (cross-module ref by ID only) |
| `ParticipantName` | `string` | Non-empty, max 200 chars |
| `ParticipantEmail` | `Email` (value object) | Valid email, case-insensitive |
| `RegistrationSource` | `RegistrationSource` | Enum: `SelfService`, `StaffAdded` |
| `RegisteredByStaffId` | `StaffUserId?` | Nullable; set when `StaffAdded` |
| `Status` | `RegistrationStatus` | Enum: `Confirmed`, `Cancelled` |
| `RegisteredAt` | `DateTimeOffset` | Non-null |
| `CancelledAt` | `DateTimeOffset?` | Nullable |
| `CancellationReason` | `string?` | Optional, max 500 chars |

**Invariants**:
- The combination (`TenantId`, `CourseId`, `ParticipantEmail`) MUST be unique among `Confirmed` registrations
- A `Cancelled` registration MUST NOT be cancelled again
- Cancellation MUST NOT be allowed after all sessions of the course have ended
- Registration MUST NOT be created for a `Cancelled` or `Completed` course
- Registration MUST NOT be created when current confirmed count >= course capacity (checked via domain service that reads course capacity)

**Domain Events**:
- `RegistrationCreated`
- `RegistrationCancelled`

### Domain Service: `RegistrationCapacityChecker`

Responsible for verifying capacity before a registration is created. Reads current confirmed registration count for a course and compares against course capacity (capacity data sourced from Courses module via an `ICourseCapacityReader` port).

---

## Cross-Module References

Modules communicate only by value (IDs), never by direct object reference or shared database joins.

| From Module | Reference | To Module | How |
|-------------|-----------|-----------|-----|
| Identity | `TenantId` | Tenants | ID only |
| Courses | `TenantId` | Tenants | ID only |
| Courses | `StaffUserId` | Identity | ID only |
| Registrations | `TenantId` | Tenants | ID only |
| Registrations | `CourseId` | Courses | ID only; capacity checked via port interface |
| Registrations | `StaffUserId` | Identity | ID only |

---

## Shared Kernel Types

Defined in `Terminar.SharedKernel`, used by all modules.

- `AggregateRoot<TId>` — base class: holds domain events, typed ID
- `Entity<TId>` — base class: typed ID, value equality
- `ValueObject` — base class: structural equality
- `IDomainEvent` — marker interface (implements `MediatR.INotification`)
- `TenantId` — strongly-typed Guid wrapper
- `Email` — value object: validated email string

---

## EF Core Schema Conventions

- Table names: plural, snake_case (e.g., `courses`, `sessions`, `registrations`)
- Column names: snake_case (e.g., `tenant_id`, `registered_at`)
- Each module has its own `DbContext` and independent EF migrations
- All `DbContext` instances configure a global query filter on `TenantId`
- `StronglyTypedId` source generator produces `CourseId`, `SessionId`, etc. with EF Core value converters

---

## State Machine Summary

### Course Status
```
Draft ──► Active ──► Completed
  │          │
  └──►  Cancelled ◄──┘
```

### Registration Status
```
Confirmed ──► Cancelled
```

### StaffUser Status
```
Active ──► Deactivated ──► Active
```

### Tenant Status
```
Active ──► Suspended ──► Active
```
