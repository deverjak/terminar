# Quickstart: Course Reservation System — Feature 001

**Phase 1 Output** | Branch: `001-course-reservation-system` | Date: 2026-03-30

This guide walks through setting up the development environment and verifying the system works end-to-end.

---

## Prerequisites

- .NET 10 SDK (`dotnet --version` → `10.x.x`)
- .NET Aspire workload (`dotnet workload install aspire`)
- Docker Desktop (for PostgreSQL via Aspire)
- An IDE: Visual Studio 2022 17.12+, Rider 2024.3+, or VS Code with C# Dev Kit

---

## 1. Clone and Restore

```bash
git clone <repo-url>
cd terminar
dotnet restore
```

---

## 2. Create the Solution via Aspire CLI

> If starting from scratch. Skip if solution already exists.

```bash
# Create Aspire AppHost + ServiceDefaults
dotnet new aspire --name Terminar --output .

# Create the API project
dotnet new web -n Terminar.Api --output src/Terminar.Api

# Create the Shared Kernel
dotnet new classlib -n Terminar.SharedKernel --output src/Terminar.SharedKernel

# Create module projects
dotnet new classlib -n Terminar.Modules.Tenants --output src/Terminar.Modules.Tenants
dotnet new classlib -n Terminar.Modules.Identity --output src/Terminar.Modules.Identity
dotnet new classlib -n Terminar.Modules.Courses --output src/Terminar.Modules.Courses
dotnet new classlib -n Terminar.Modules.Registrations --output src/Terminar.Modules.Registrations

# Add all projects to solution
dotnet sln add src/Terminar.Api/Terminar.Api.csproj
dotnet sln add src/Terminar.SharedKernel/Terminar.SharedKernel.csproj
dotnet sln add src/Terminar.Modules.Tenants/Terminar.Modules.Tenants.csproj
dotnet sln add src/Terminar.Modules.Identity/Terminar.Modules.Identity.csproj
dotnet sln add src/Terminar.Modules.Courses/Terminar.Modules.Courses.csproj
dotnet sln add src/Terminar.Modules.Registrations/Terminar.Modules.Registrations.csproj
```

---

## 3. Run the Application

```bash
# From the AppHost project directory
cd src/Terminar.AppHost
dotnet run
```

Aspire starts:
- PostgreSQL in Docker on a dynamic port
- The `Terminar.Api` service
- Aspire Dashboard at `https://localhost:15888` (URL shown in console)

---

## 4. Apply Migrations

Each module has its own EF Core migrations. Run per module:

```bash
# From the repo root
dotnet ef migrations add InitialCreate \
  --project src/Terminar.Modules.Tenants \
  --startup-project src/Terminar.Api \
  --context TenantsDbContext

dotnet ef migrations add InitialCreate \
  --project src/Terminar.Modules.Identity \
  --startup-project src/Terminar.Api \
  --context IdentityDbContext

dotnet ef migrations add InitialCreate \
  --project src/Terminar.Modules.Courses \
  --startup-project src/Terminar.Api \
  --context CoursesDbContext

dotnet ef migrations add InitialCreate \
  --project src/Terminar.Modules.Registrations \
  --startup-project src/Terminar.Api \
  --context RegistrationsDbContext
```

Migrations are applied automatically on startup via `DbContext.Database.MigrateAsync()` in the `Program.cs`.

---

## 5. Create the First Tenant

```bash
curl -s -X POST http://localhost:5000/api/v1/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $(dotnet user-jwts create --role SystemAdmin --output token)" \
  -d '{
    "name": "Coding Academy Prague",
    "slug": "coding-academy-prague",
    "default_language_code": "cs",
    "admin_username": "admin",
    "admin_email": "admin@example.com",
    "admin_password": "Admin@12345"
  }'
```

---

## 6. Login as Staff

```bash
curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin@12345"
  }' | jq .access_token
```

Store the returned token as `$TOKEN`.

---

## 7. Create a Course

```bash
curl -s -X POST http://localhost:5000/api/v1/courses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title": "Test Course",
    "description": "A quick test.",
    "course_type": "OneTime",
    "registration_mode": "Open",
    "capacity": 10,
    "sessions": [{
      "scheduled_at": "2026-12-01T09:00:00+01:00",
      "duration_minutes": 120,
      "location": "Room 1"
    }]
  }'
```

---

## 8. Self-Register as a Participant

```bash
COURSE_ID="<uuid-from-step-7>"

curl -s -X POST http://localhost:5000/api/v1/courses/$COURSE_ID/registrations \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: coding-academy-prague" \
  -d '{
    "participant_name": "Jan Novák",
    "participant_email": "jan.novak@example.com"
  }'
```

---

## 9. View the Roster

```bash
curl -s http://localhost:5000/api/v1/courses/$COURSE_ID/registrations \
  -H "Authorization: Bearer $TOKEN" | jq .
```

Expected: roster with 1 confirmed registration for Jan Novák.

---

## Validation Checklist

- [ ] `dotnet run` starts without errors
- [ ] Aspire Dashboard shows API and PostgreSQL as healthy
- [ ] Migrations apply cleanly (no errors in console)
- [ ] Tenant creation returns `201 Created`
- [ ] Staff login returns a JWT token
- [ ] Course creation returns `201 Created` with correct session data
- [ ] Self-registration returns `201 Created`
- [ ] Duplicate registration returns `409 Conflict`
- [ ] Registration beyond capacity returns `422 Unprocessable Entity`
- [ ] Roster endpoint returns registered participant

---

## Running Tests

```bash
# Unit tests
dotnet test tests/Terminar.Modules.Courses.Tests
dotnet test tests/Terminar.Modules.Registrations.Tests

# Integration tests (requires Docker for PostgreSQL)
dotnet test tests/Terminar.Api.IntegrationTests
```
