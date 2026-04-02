# Developer Quickstart: 004-enrollment-email-excusals

## What This Feature Adds

1. **Real SMTP email** via MailKit (replaces stub).
2. **Participant portal** — public magic-link login flow (`/participant`), safe-link course view, excusal submission, unenrollment.
3. **Excusal credits** — redeemable makeup credits, scoped by course tags and tenant validity windows.
4. **Staff excusal management** — extend/re-tag/soft-delete credits with full audit log.
5. **Tenant excusal settings** — global defaults + per-course overrides.

---

## Key Config to Add

In `src/Terminar.Api/appsettings.Development.json`, add:

```json
"Smtp": {
  "Host": "localhost",
  "Port": 1025,
  "Username": "",
  "Password": "",
  "FromAddress": "dev@terminar.local",
  "FromName": "Termínář Dev",
  "UseSsl": false,
  "UseStartTls": false
}
```

For local dev, run [MailHog](https://github.com/mailhog/MailHog) or [Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP) to catch outbound emails.

---

## New API Routes (quick reference)

### Public (no auth, needs `X-Tenant-Id` header)

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/v1/participants/magic-link` | Request magic link email |
| `POST` | `/api/v1/participants/portal/redeem` | Exchange magic link token → portal token |
| `GET` | `/api/v1/participants/portal` | Participant dashboard (X-Portal-Token) |
| `GET` | `/api/v1/participants/courses/{safeLinkToken}` | Course view via safe link |
| `POST` | `/api/v1/participants/courses/{safeLinkToken}/unenroll` | Self-unenroll |
| `POST` | `/api/v1/participants/courses/{safeLinkToken}/sessions/{id}/excuse` | Excuse from session |
| `POST` | `/api/v1/participants/credits/{id}/redeem` | Redeem excusal credit (X-Portal-Token) |

### Staff (Bearer token)

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/excusal-credits` | List credits |
| `PATCH` | `/api/v1/excusal-credits/{id}` | Extend / re-tag |
| `DELETE` | `/api/v1/excusal-credits/{id}` | Soft-delete |
| `GET/POST/PATCH/DELETE` | `/api/v1/settings/excusal-windows` | Validity window CRUD |
| `GET/PATCH` | `/api/v1/settings/excusal-policy` | Tenant excusal settings |
| `GET/PATCH` | `/api/v1/courses/{id}/excusal-policy` | Per-course policy |

---

## New Frontend Routes

| URL | Description |
|---|---|
| `/participant` | Public magic link request page |
| `/participant/portal` | Participant dashboard |
| `/participant/course/:token` | Course-specific view |
| `/app/settings/excusal` | Staff: excusal settings |
| `/app/excusal-credits` | Staff: credit management |
| `/app/courses/:id/excusal-policy` | Staff: per-course policy |

---

## Database Migrations (run after pulling)

```bash
# Registrations module
dotnet ef migrations add EnrollmentEmailExcusals \
  --project src/Terminar.Modules.Registrations \
  --startup-project src/Terminar.Api \
  --context RegistrationsDbContext

# Courses module
dotnet ef migrations add CourseExcusalPolicy \
  --project src/Terminar.Modules.Courses \
  --startup-project src/Terminar.Api \
  --context CoursesDbContext

# Tenants module
dotnet ef migrations add TenantExcusalSettings \
  --project src/Terminar.Modules.Tenants \
  --startup-project src/Terminar.Api \
  --context TenantsDbContext
```

Migrations are applied automatically on startup via `DatabaseMigrationService`.

---

## New NuGet Packages

```bash
cd src/Terminar.Api
dotnet add package MailKit
dotnet add package MimeKit
```

---

## Testing Participant Flow Manually

1. Create a tenant + enroll a participant via staff API.
2. Call `POST /api/v1/participants/magic-link` with the participant's email and `X-Tenant-Id`.
3. Check MailHog (http://localhost:8025) for the magic link email.
4. Copy the token from the link.
5. Call `POST /api/v1/participants/portal/redeem` with the token.
6. Use the returned `portalToken` in `X-Portal-Token` header for subsequent calls.
