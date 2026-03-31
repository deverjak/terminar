# Research: Course Reservation System — Feature 001

**Phase 0 Output** | Branch: `001-course-reservation-system` | Date: 2026-03-30

---

## 1. .NET Aspire Version & Setup

**Decision**: Use .NET Aspire latest stable (9.x line; verify exact patch at implementation time via `dotnet workload search aspire`).

**Rationale**: .NET Aspire follows its own release cadence independent of .NET runtime. The 9.x line is the stable series for .NET 9/10. AppHost and ServiceDefaults are created via `dotnet new` templates.

**CLI Commands**:
```bash
dotnet new aspire --name Terminar              # Creates AppHost + ServiceDefaults
# OR separately:
dotnet new aspire-apphost -n Terminar.AppHost
dotnet new aspire-servicedefaults -n Terminar.ServiceDefaults
```

**Alternatives considered**: Manual project setup — rejected because Aspire CLI wires OpenTelemetry, health checks, and service discovery automatically.

---

## 2. Modular Monolith Project Structure

**Decision**: One C# project per module, with `Domain/`, `Application/`, and `Infrastructure/` folders inside each project. DDD layer dependencies enforced by convention and code review.

**Rationale**: Splitting each module into 3 separate class library projects (Domain/Application/Infrastructure) gives compile-time enforcement but creates 12+ projects for 4 modules. For the initial implementation, folder-based separation is pragmatic, still DDD-compliant, and can evolve to separate projects later without architectural change. The shared kernel is a dedicated project.

**Structure**:
```
src/
├── Terminar.AppHost/
├── Terminar.ServiceDefaults/
├── Terminar.SharedKernel/           ← DDD base classes, no business logic
├── Terminar.Api/                    ← HTTP entry point, DI composition root
├── Terminar.Modules.Tenants/
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Terminar.Modules.Identity/
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Terminar.Modules.Courses/
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
└── Terminar.Modules.Registrations/
    ├── Domain/
    ├── Application/
    └── Infrastructure/
```

**Alternatives considered**: Separate projects per layer — rejected as too much boilerplate for v1; separate projects per module only (no layer folders) — rejected as too loose for DDD enforcement.

---

## 3. Multi-Tenancy with EF Core + PostgreSQL

**Decision**: Shared PostgreSQL database, schema-per-module, with EF Core global query filters enforcing tenant isolation.

**Rationale**: Schema-per-module isolates each bounded context's tables while sharing one database instance (lower operational cost). Global query filters on EF Core entities ensure no query can return data from a different tenant without explicit override.

**Pattern**:
```csharp
// TenantId is a field on every aggregate root
modelBuilder.Entity<Course>()
    .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
```

`ITenantContext` is injected into each `DbContext`. The tenant is resolved from the JWT claim at the HTTP layer and set in `IHttpContextAccessor`.

**PostgreSQL Schemas**:
- `tenants` schema → Tenants module tables
- `identity` schema → Identity module tables
- `courses` schema → Courses module tables
- `registrations` schema → Registrations module tables

**Alternatives considered**: Database-per-tenant — rejected for v1 (higher cost, operational complexity); application-level filtering without EF filters — rejected (error-prone, easy to forget).

---

## 4. CQRS Tooling

**Decision**: MediatR 12.x for command/query dispatch and domain event publication.

**Current version**: MediatR 12.x (v12.1+). No longer requires a separate `MediatR.Extensions.Microsoft.DependencyInjection` package — built-in DI support.

**Rationale**: MediatR provides clean separation of command/query handling, pipeline behaviors for cross-cutting concerns (validation, logging, transactions), and doubles as the domain event publisher via `INotification`. Well-understood by .NET community.

**Alternatives considered**: Custom `ICommandHandler<T>` / `IQueryHandler<T>` — more boilerplate, no pipeline; Wolverine — richer but heavier, overkill for v1.

---

## 5. Identity Framework Choice

**Decision**: ASP.NET Core Identity — used **only in the Infrastructure layer** of the Identity module. Domain aggregate `StaffUser` remains framework-free.

**Rationale**: ASP.NET Core Identity provides battle-tested password hashing (PBKDF2), account lockout, and token storage out of the box. Using it only in Infrastructure keeps the domain clean — `AppIdentityUser : IdentityUser` is a persistence concern, mapped to/from the domain `StaffUser` aggregate by `StaffUserRepository`. The domain never imports `Microsoft.AspNetCore.Identity`.

**What it replaces**: BCrypt.Net-Next (no longer needed — Identity handles hashing), custom refresh token entity (replaced by Identity's token store via `UserManager`).

**Alternatives considered**: Custom from-scratch (Option A) — rejected because security-sensitive credential code benefits from a well-audited framework; Keycloak/Auth0 — out of scope for v1.

---

## 6. JWT Authentication

**Decision**: `Microsoft.AspNetCore.Authentication.JwtBearer` (built into ASP.NET Core, no additional NuGet needed). Issue tokens from a dedicated login endpoint in the Identity module.

**Pattern**: Staff POSTs credentials → Identity module validates, issues signed JWT containing `TenantId`, `UserId`, `Role`. All subsequent requests include `Authorization: Bearer <token>`.

**Local dev**: Use `dotnet user-jwts` CLI tool (built into .NET 8+) for generating test tokens without running an auth server.

**Alternatives considered**: External OIDC provider (Keycloak, Auth0) — out of scope for v1; API keys — not suitable for user-based authentication with roles.

---

## 6. Domain Events

**Decision**: In-process domain events via MediatR `INotification` + `INotificationHandler<T>`. Events dispatched after `SaveChangesAsync` succeeds (post-commit dispatch).

**Rationale**: Simple, no external dependencies. For v1, eventual consistency within the same database transaction is acceptable. Cross-module side effects (e.g., Registrations module reacting to a Course cancelled event) are handled by notification handlers in the relevant module.

**Pattern**:
- Aggregates accumulate domain events in a `List<IDomainEvent>` property
- `DbContext.SaveChangesAsync` override dispatches events via `IMediator` after persisting
- Handlers are in the Application layer of the subscribing module

**Alternatives considered**: Transactional outbox — more reliable (no event loss on crash), but more complex; deferred for v2 if reliability becomes a concern.

---

## 7. Key NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `MediatR` | 12.x | CQRS + domain event dispatch |
| `FluentValidation` | 12.x | Request/command validation (pipeline behavior) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.x | EF Core PostgreSQL provider for .NET 10 |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | latest 9.x | Aspire-integrated EF Core + PostgreSQL |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | (framework) | JWT auth — no separate package needed |
| `BCrypt.Net-Next` | 4.x | Password hashing for staff credentials |
| `StronglyTypedId` | 2.x | Source-generated strongly-typed IDs (Guid wrappers) |

---

## 8. .NET Aspire PostgreSQL Integration

**Decision**: Use `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` in each service project. Declare PostgreSQL resource in AppHost; inject into service projects via named references.

**AppHost pattern**:
```csharp
var postgres = builder.AddPostgres("postgres").WithDataVolume();
var db = postgres.AddDatabase("terminar-db");

builder.AddProject<Projects.Terminar_Api>("api")
    .WithReference(db)
    .WaitFor(db);
```

**Service project pattern**:
```csharp
// Each module DbContext registered separately by connection name
builder.AddNpgsqlDbContext<CoursesDbContext>("terminar-db");
builder.AddNpgsqlDbContext<RegistrationsDbContext>("terminar-db");
```

All DbContexts share one database but use separate schemas and have independent migrations per module.

**Aspire version note**: Verify exact Aspire package version with `dotnet workload search aspire` at implementation time. The 9.x line is the current stable series.
