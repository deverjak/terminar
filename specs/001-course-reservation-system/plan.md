# Implementation Plan: Course Reservation System (Termínář)

**Branch**: `001-course-reservation-system` | **Date**: 2026-03-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-course-reservation-system/spec.md`

---

## Summary

Build the backend REST API for Termínář — a multi-tenant course reservation system. The backend is a .NET 10 / ASP.NET Core modular monolith with DDD, exposing a JSON REST API. It manages four bounded contexts (Tenants, Identity, Courses, Registrations), persists data to PostgreSQL, and is orchestrated via .NET Aspire. Multi-language support is out of scope — handled by the frontend.

---

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: ASP.NET Core 10 (Minimal APIs), ASP.NET Core Identity (Infrastructure layer of Identity module), MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql), .NET Aspire (9.x latest stable), StronglyTypedId 2.x
**Storage**: PostgreSQL (single database, schema-per-module: `tenants`, `identity`, `courses`, `registrations`)
**Testing**: xUnit, Testcontainers.PostgreSQL (integration tests), FluentAssertions
**Target Platform**: Linux server / Docker (via Aspire); dev on Windows with Docker Desktop
**Project Type**: REST API — modular monolith
**Performance Goals**: < 200ms p95 for typical read/write operations; standard web API expectations
**Constraints**: Tenant isolation enforced at domain level (EF Core global query filters); no cross-module DB joins; dependency direction: Infrastructure → Application → Domain
**Scale/Scope**: Single tenant in v1 deployment; architecture supports N tenants from day one

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

| Principle | Status | Evidence |
|-----------|--------|---------|
| **I. Domain-Driven Design** | ✅ PASS | 4 modules map to bounded contexts; aggregates defined in data-model.md; domain events modeled; repos as interfaces in Domain layer |
| **II. Multi-Tenancy by Default** | ✅ PASS | `TenantId` on all aggregate roots; EF Core global query filters; cross-module refs by ID only |
| **III. Multi-Language First** | ✅ SCOPED OUT | User explicitly deferred i18n to frontend; backend returns raw content strings; no hardcoded user-facing strings in backend responses |
| **IV. Clean Architecture** | ✅ PASS | Each module has Domain/, Application/, Infrastructure/ folders; SharedKernel has no infra deps; module projects only reference SharedKernel |
| **V. Spec-First Development** | ✅ PASS | spec.md approved; plan derived from spec |

### Post-Design Gate (re-check after Phase 1)

| Principle | Status | Evidence |
|-----------|--------|---------|
| **I. DDD** | ✅ PASS | data-model.md: aggregates, value objects, domain events, repository interfaces defined; no business logic in Infrastructure |
| **II. Multi-Tenancy** | ✅ PASS | All aggregates have `TenantId`; cross-module communication by ID; tenant resolved from JWT at API boundary |
| **III. Multi-Language** | ✅ SCOPED | Backend agnostic; strings stored as-is |
| **IV. Clean Architecture** | ✅ PASS | API contracts use DTOs in Application layer; EF entities in Infrastructure; domain has zero external deps |
| **V. Spec-First** | ✅ PASS | All API contracts derived from spec user stories |

**No violations. No Complexity Tracking entries needed.**

---

## Project Structure

### Documentation (this feature)

```text
specs/001-course-reservation-system/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   └── api.md           ← Phase 1 output
└── tasks.md             ← Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
Terminar.sln

src/
├── Terminar.AppHost/                         # .NET Aspire AppHost — orchestration entry point
│   └── Program.cs                            # Declares PostgreSQL resource, API project refs
│
├── Terminar.ServiceDefaults/                 # Aspire shared defaults (observability, health checks)
│   └── Extensions.cs
│
├── Terminar.SharedKernel/                    # DDD base classes — zero business logic
│   ├── AggregateRoot.cs
│   ├── Entity.cs
│   ├── ValueObject.cs
│   ├── IDomainEvent.cs
│   └── ValueObjects/
│       ├── TenantId.cs
│       └── Email.cs
│
├── Terminar.Api/                             # HTTP entry point — DI composition root only
│   ├── Program.cs                            # Wires all modules, Aspire service defaults
│   ├── Middleware/
│   │   └── TenantResolutionMiddleware.cs     # Resolves tenant from JWT or X-Tenant-Id header
│   └── Modules/                              # Module route registration (one file per module)
│       ├── TenantsModule.cs
│       ├── IdentityModule.cs
│       ├── CoursesModule.cs
│       └── RegistrationsModule.cs
│
├── Terminar.Modules.Tenants/
│   ├── Domain/
│   │   ├── Tenant.cs                         # Aggregate root
│   │   ├── TenantStatus.cs
│   │   ├── Events/
│   │   │   └── TenantCreated.cs
│   │   └── Repositories/
│   │       └── ITenantRepository.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   └── CreateTenant/
│   │   │       ├── CreateTenantCommand.cs
│   │   │       ├── CreateTenantCommandHandler.cs
│   │   │       └── CreateTenantCommandValidator.cs
│   │   └── Queries/
│   │       └── GetTenant/
│   │           ├── GetTenantQuery.cs
│   │           └── GetTenantQueryHandler.cs
│   └── Infrastructure/
│       ├── TenantsDbContext.cs
│       ├── TenantsDbContextFactory.cs        # For EF migrations CLI
│       ├── Repositories/
│       │   └── TenantRepository.cs
│       ├── Migrations/
│       └── TenantsModule.cs                  # IServiceCollection extension (DI registration)
│
├── Terminar.Modules.Identity/
│   ├── Domain/
│   │   ├── StaffUser.cs                      # Aggregate root (pure domain — no ASP.NET Identity refs)
│   │   ├── StaffRole.cs
│   │   ├── StaffUserStatus.cs
│   │   ├── Events/
│   │   │   └── StaffUserCreated.cs
│   │   └── Repositories/
│   │       └── IStaffUserRepository.cs       # Domain interface — returns StaffUser, not IdentityUser
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── CreateStaffUser/
│   │   │   └── DeactivateStaffUser/
│   │   ├── Queries/
│   │   │   └── ListStaffUsers/
│   │   └── Auth/
│   │       ├── LoginCommand.cs
│   │       ├── LoginCommandHandler.cs        # Uses IStaffUserRepository; delegates credential check to UserManager<AppIdentityUser>
│   │       └── RefreshTokenCommand.cs
│   └── Infrastructure/
│       ├── Identity/
│       │   ├── AppIdentityUser.cs            # IdentityUser subclass — infra concern only
│       │   └── AppIdentityDbContext.cs       # IdentityDbContext<AppIdentityUser> — owns ASP.NET Identity tables
│       ├── Persistence/
│       │   └── StaffUserRepository.cs        # Maps AppIdentityUser ↔ StaffUser domain aggregate
│       ├── Services/
│       │   └── JwtTokenService.cs            # Issues + validates JWT; uses UserManager for claims
│       ├── Migrations/
│       └── IdentityModule.cs                 # Registers AddIdentityCore<AppIdentityUser>, AddJwtBearer, etc.
│
├── Terminar.Modules.Courses/
│   ├── Domain/
│   │   ├── Course.cs                         # Aggregate root
│   │   ├── Session.cs                        # Child entity
│   │   ├── CourseType.cs
│   │   ├── CourseStatus.cs
│   │   ├── RegistrationMode.cs
│   │   ├── Events/
│   │   │   ├── CourseCreated.cs
│   │   │   ├── CourseActivated.cs
│   │   │   └── CourseCancelled.cs
│   │   └── Repositories/
│   │       └── ICourseRepository.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── CreateCourse/
│   │   │   ├── UpdateCourse/
│   │   │   └── CancelCourse/
│   │   ├── Queries/
│   │   │   ├── ListCourses/
│   │   │   └── GetCourse/
│   │   └── Ports/
│   │       └── ICourseCapacityReader.cs      # Port used by Registrations module
│   └── Infrastructure/
│       ├── CoursesDbContext.cs
│       ├── Repositories/
│       │   └── CourseRepository.cs
│       ├── Ports/
│       │   └── CourseCapacityReader.cs       # Implements ICourseCapacityReader
│       ├── Migrations/
│       └── CoursesModule.cs
│
└── Terminar.Modules.Registrations/
    ├── Domain/
    │   ├── Registration.cs                   # Aggregate root
    │   ├── RegistrationStatus.cs
    │   ├── RegistrationSource.cs
    │   ├── Events/
    │   │   ├── RegistrationCreated.cs
    │   │   └── RegistrationCancelled.cs
    │   ├── Services/
    │   │   └── RegistrationCapacityChecker.cs  # Domain service — checks capacity via port
    │   └── Repositories/
    │       └── IRegistrationRepository.cs
    ├── Application/
    │   ├── Commands/
    │   │   ├── CreateRegistration/
    │   │   └── CancelRegistration/
    │   └── Queries/
    │       └── GetCourseRoster/
    └── Infrastructure/
        ├── RegistrationsDbContext.cs
        ├── Repositories/
        │   └── RegistrationRepository.cs
        ├── Migrations/
        └── RegistrationsModule.cs

tests/
├── Terminar.Modules.Courses.Tests/           # Domain + Application unit tests
├── Terminar.Modules.Registrations.Tests/     # Domain + Application unit tests
└── Terminar.Api.IntegrationTests/            # Full API integration tests (Testcontainers)
```

**Structure Decision**: Modular monolith — one C# class library project per bounded context. DDD layers (Domain / Application / Infrastructure) enforced as folders within each module project. No cross-module project references except via SharedKernel. The `Terminar.Api` project is a pure composition root: it holds no business logic, only DI wiring and route registration.

---

## Key Design Decisions

### Tenant Resolution

- **Authenticated requests** (staff): `TenantId` extracted from JWT `tenant_id` claim by `TenantResolutionMiddleware` → stored in `ITenantContext` (scoped service)
- **Public requests** (self-registration, course browsing): `TenantId` resolved from `X-Tenant-Id` header (slug or GUID) → validated against Tenants module
- All EF Core `DbContext` instances receive `ITenantContext` via constructor injection and apply global query filters

### CQRS Pattern

Every use case is a MediatR `IRequest<T>` (command or query). Pipeline behaviors handle:
1. `ValidationBehavior<TRequest, TResponse>` — runs FluentValidation validators
2. `LoggingBehavior<TRequest, TResponse>` — logs command/query execution time
3. (Future) `TransactionBehavior` — wraps commands in a DB transaction

### Domain Events

Aggregates raise domain events (stored in a `_domainEvents` list). The EF Core `DbContext.SaveChangesAsync` override dispatches events via `IMediator.Publish()` after successfully committing. Handlers are `INotificationHandler<TEvent>` implementations in the Application layer.

### Cross-Module Communication

The Registrations module needs course capacity data from the Courses module. This is solved via a port interface:
- `ICourseCapacityReader` defined in `Terminar.Modules.Courses/Application/Ports/`
- `CourseCapacityReader` implemented in `Terminar.Modules.Courses/Infrastructure/Ports/`
- Registered in DI and injected into `RegistrationCapacityChecker` domain service
- No direct DbContext sharing between modules

### Authentication — ASP.NET Core Identity in Infrastructure

ASP.NET Core Identity is used **only inside the Infrastructure layer** of the Identity module. The domain layer has zero knowledge of it.

**Layer responsibilities**:
- **Domain**: `StaffUser` aggregate — owns business rules (status, role, tenant)
- **Application**: `LoginCommandHandler` — calls `IStaffUserRepository` to find user, delegates credential verification to `UserManager<AppIdentityUser>` via an `IIdentityAuthService` port
- **Infrastructure**:
  - `AppIdentityUser : IdentityUser` — maps to ASP.NET Identity tables (`identity.AspNetUsers`, etc.)
  - `AppIdentityDbContext : IdentityDbContext<AppIdentityUser>` — handles Identity table schema in `identity` schema
  - `StaffUserRepository` — reads `AppIdentityUser` via `UserManager<AppIdentityUser>`, maps to/from domain `StaffUser`
  - `JwtTokenService` — issues JWT with claims (`sub`, `tenant_id`, `role`, `exp`, `jti`); validates and rotates refresh tokens

**What ASP.NET Core Identity provides for free**:
- Password hashing (PBKDF2, no BCrypt dependency needed)
- Password policy validation (min length, complexity)
- Account lockout after N failed attempts
- `UserManager<T>` / `SignInManager<T>` API

**JWT**: HS256 (configurable to RS256). Refresh tokens stored as ASP.NET Identity user tokens (`UserManager.SetAuthenticationTokenAsync`).

**Local dev**: `dotnet user-jwts create --role Staff` for generating test tokens without a running server.

### EF Core Migrations

Each module has independent migrations. Applied automatically on startup via `IHostedService`. Migration files live in `Infrastructure/Migrations/` within each module project.

---

## Complexity Tracking

> No constitution violations — section intentionally empty.
