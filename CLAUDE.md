# terminar Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-03

## Active Technologies
- TypeScript 5.x, React 19, Node.js 22+ + Mantine v9, React Router v7, TanStack Query v5, react-i18next, Vite 6 (002-web-ui-frontend)
- No database — all state via backend API; access token in memory, refresh token + tenant slug in localStorage (002-web-ui-frontend)
- TypeScript 5.x, C# 14 / .NET 10 + React 19, Mantine v9, TanStack Query v5, react-i18next, React Router v7, Vite 6 (003-recurrent-course-creation)
- No new storage — sessions are transient form state submitted to the existing backend API; backend uses PostgreSQL via EF Core (003-recurrent-course-creation)
- C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend) + ASP.NET Core 10 Minimal APIs, MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql), MailKit + MimeKit (new), React 19, Mantine v9, TanStack Query v5, react-i18next (004-enrollment-email-excusals)
- PostgreSQL via EF Core (existing); new tables in `registrations`, `courses`, `tenants` schemas (004-enrollment-email-excusals)
- C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend) + ASP.NET Core 10 Minimal APIs, MediatR 12.x (backend); Mantine v9, TanStack Query v5, react-i18next (frontend) (005-courses-filtering-pagination)
- PostgreSQL via EF Core 10 — no new tables or migrations required (005-courses-filtering-pagination)

- C# 14 / .NET 10 + ASP.NET Core 10 (Minimal APIs), ASP.NET Core Identity (Infrastructure only), MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql), .NET Aspire (9.x latest stable), StronglyTypedId 2.x (001-course-reservation-system)

## Project Structure

```text
src/
  Terminar.AppHost/          ← .NET Aspire orchestration entry point
  Terminar.ServiceDefaults/  ← Aspire shared defaults (health, telemetry)
  Terminar.SharedKernel/     ← DDD base classes (AggregateRoot, Entity, ValueObject, IDomainEvent)
  Terminar.Api/              ← HTTP composition root — no business logic here
  Terminar.Modules.Tenants/        ← Tenants bounded context (Domain/ Application/ Infrastructure/)
  Terminar.Modules.Identity/       ← Staff auth bounded context
  Terminar.Modules.Courses/        ← Courses bounded context
  Terminar.Modules.Registrations/  ← Registrations bounded context
tests/
  Terminar.Modules.Courses.Tests/
  Terminar.Modules.Registrations.Tests/
  Terminar.Api.IntegrationTests/
```

## Commands

```bash
# Run application (starts Docker + PostgreSQL via Aspire)
cd src/Terminar.AppHost && dotnet run

# Run all tests
dotnet test

# Add EF migration (example for Courses module)
dotnet ef migrations add <MigrationName> \
  --project src/Terminar.Modules.Courses \
  --startup-project src/Terminar.Api \
  --context CoursesDbContext

# Generate local dev JWT
dotnet user-jwts create --role Staff
```

## Code Style

- DDD strict: business logic lives in Domain layer only; Application layer orchestrates; Infrastructure implements interfaces
- Dependency direction: Infrastructure → Application → Domain (never reversed)
- All aggregate roots have `TenantId` (multi-tenancy enforced via EF Core global query filters)
- Use `StronglyTypedId` source generator for all entity IDs (e.g., `CourseId`, `RegistrationId`)
- CQRS via MediatR: every use case is a `IRequest<T>`; pipeline behaviors for validation + logging
- Domain events: raised in aggregates, dispatched after `SaveChangesAsync` via `IMediator.Publish`
- Cross-module communication: by value (IDs) only — no shared DbContexts, no direct object references
- Minimal APIs in `Terminar.Api` — route registration only, delegate to MediatR commands/queries

## Recent Changes
- 005-courses-filtering-pagination: Added C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend) + ASP.NET Core 10 Minimal APIs, MediatR 12.x (backend); Mantine v9, TanStack Query v5, react-i18next (frontend)
- 004-enrollment-email-excusals: Added C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend) + ASP.NET Core 10 Minimal APIs, MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql), MailKit + MimeKit (new), React 19, Mantine v9, TanStack Query v5, react-i18next
- 003-recurrent-course-creation: Added TypeScript 5.x, C# 14 / .NET 10 + React 19, Mantine v9, TanStack Query v5, react-i18next, React Router v7, Vite 6


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
