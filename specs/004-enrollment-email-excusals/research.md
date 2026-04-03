# Research: Course Enrollment Email and Session Excusals

**Feature**: `004-enrollment-email-excusals`  
**Phase**: 0 — Research  
**Date**: 2026-04-02

---

## Decision 1 — SMTP Library

**Decision**: Use **MailKit** (`MailKit` + `MimeKit` NuGet packages) for SMTP email sending.

**Rationale**: `System.Net.Mail.SmtpClient` is marked as legacy in .NET docs; Microsoft recommends MailKit for new development. MailKit supports modern SMTP auth mechanisms (PLAIN, LOGIN, OAUTH2), correct STARTTLS/SSL handling, and has proper async support. MimeKit provides HTML email construction with plain-text fallback.

**Configuration** — new `SmtpSettings` section in `appsettings.json`:
```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "noreply@example.com",
  "Password": "...",
  "FromAddress": "noreply@example.com",
  "FromName": "Termínář",
  "UseSsl": false,
  "UseStartTls": true
}
```

**Alternatives considered**: System.Net.Mail (legacy, rejected), SendGrid SDK (third-party SaaS, out of scope), Fluentemail (abstraction layer, unnecessary complexity for this scope).

---

## Decision 2 — Email Templating

**Decision**: Plain C# string interpolation for email HTML bodies with an `IEmailTemplateRenderer` interface per email type. No third-party templating engine.

**Rationale**: The number of email types is small (≤6). A lightweight method-per-template approach is sufficient, testable, and avoids a new dependency. Templates are C# methods returning `(string subject, string htmlBody, string textBody)`. If complexity grows, swapping to Scriban/Razor is a one-interface-implementation change.

**Alternatives considered**: Razor templates (requires full MVC, overkill), Scriban (good, but adds a dependency; deferred), Fluid (same).

---

## Decision 3 — Participant Portal Authentication

**Decision**: Two-token model:

1. **Magic Link Token** (challenge token): 32-byte cryptographically random, URL-safe base64, 15-minute TTL, single-use. Sent in email. Exchange endpoint issues a portal session token.
2. **Portal Session Token**: 32-byte cryptographically random, URL-safe base64, 7-day TTL, multi-use (revocable by re-request). Returned to frontend after magic link redemption; stored in `localStorage` like a staff JWT.
3. **Course Safe Link Token**: existing `SelfCancellationToken` on `Registration` (Guid, permanent while enrollment is active). Repurposed/renamed to `SafeLinkToken`. Grants access to that specific course's participant view.

**Auth flow**:
```
1. Participant enters email on /participant page
2. POST /api/v1/participants/magic-link → system looks up enrollments, sends magic link email (always 202)
3. Participant clicks link → GET /participant/portal?token={magicLinkToken}
4. Frontend calls POST /api/v1/participants/portal/redeem { token } → { portalToken, expiresAt }
5. Frontend stores portalToken in localStorage
6. Portal calls use X-Portal-Token: {portalToken} header
```

**Rationale**: Stateless safe link tokens for course views (consistent with existing pattern); stateful portal token for multi-course dashboard (required by spec User Story 2 and 5). No cookies needed — consistent with the existing token-in-memory/localStorage pattern for staff auth.

**Alternatives considered**: JWT-signed tokens (no revocation without DB lookup anyway, and adds key management), cookie-based session (requires CSRF protection, out of scope).

---

## Decision 4 — Course Tags Storage

**Decision**: `List<string>` stored as a PostgreSQL `text[]` array column on `CourseExcusalPolicy` (owned entity of Course). Managed via EF Core value conversion to JSON string or native array type.

**Rationale**: Tags are simple strings with no lifecycle of their own. No need for a join table. Query-time matching (is any tag shared between credit and course?) is done in application code after loading the relevant courses. At the expected scale (hundreds of courses per tenant), application-side filtering is fine.

**Alternatives considered**: Separate `course_tags` join table (overkill for simple strings), PostgreSQL JSONB (works but native array is more idiomatic for a flat string list).

---

## Decision 5 — No-Tags Course Credit Policy

**Decision**: If a course has no tags assigned (empty tag list), excusal credit generation is **disabled** for that course regardless of global or local policy toggle.

**Rationale**: A credit with an empty tag set would match all courses in the tenant (every course trivially shares "at least one tag" when intersection is empty-set). This is almost certainly unintentional. Forcing deliberate tag assignment prevents misuse. Staff attempting to generate credits for an untagged course receives a validation error with a clear message.

---

## Decision 6 — Excusal & ExcusalCredit Module Placement

**Decision**: 
- `Excusal` aggregate → `Terminar.Modules.Registrations` (schema: `registrations`)
- `ExcusalCredit` aggregate → `Terminar.Modules.Registrations` (schema: `registrations`)
- `ExcusalCreditAuditEntry` entity → `Terminar.Modules.Registrations` (schema: `registrations`)
- `ParticipantMagicLink` aggregate → `Terminar.Modules.Registrations` (schema: `registrations`)
- `CourseExcusalPolicy` value object/owned entity → `Terminar.Modules.Courses` (on Course aggregate)
- `ExcusalValidityWindow` aggregate → `Terminar.Modules.Tenants` (schema: `tenants`)
- `TenantExcusalSettings` owned entity → `Terminar.Modules.Tenants` (on Tenant aggregate)

**Rationale**: Excusal and credit relate to the participant-registration relationship; they belong in Registrations. Course configuration belongs in Courses. Tenant-wide policy belongs in Tenants. No new module needed — avoids adding complexity to the project structure for a feature that naturally fits existing module boundaries. Cross-module references are by ID only (no shared DbContext).

---

## Decision 7 — Excusal Validity Window Ordering

**Decision**: Windows are ordered by `StartDate` ascending. No explicit sort order field.

**Rationale**: Validity windows are inherently temporal; date ordering is natural and unambiguous. An explicit sort order would require admin maintenance with no benefit.

---

## Decision 8 — SafeLinkToken Rename

**Decision**: Rename `Registration.SelfCancellationToken` to `Registration.SafeLinkToken` (data migration required). The token serves as the access credential for all participant self-service actions on that specific course enrollment: view sessions, unenroll, excuse, redeem credit.

**Rationale**: The token now enables multiple self-service actions beyond cancellation. The name `SafeLinkToken` is the canonical term used throughout the spec and better reflects its expanded role.

---

## Decision 9 — Magic Link Tenant Resolution

**Decision**: The magic link request endpoint (`POST /api/v1/participants/magic-link`) uses the existing tenant resolution middleware (X-Tenant-Id header or slug). Tenant context is required — a participant can have enrollments in multiple tenants and the portal is scoped to one tenant at a time.

**Rationale**: Consistent with existing middleware; participants access their courses within a tenant's context. The frontend must include the tenant slug/ID when making the magic link request (e.g., embedded in the `/participant` page URL as a query param or subdomain).

---

## Resolved Spec Ambiguity — No-Tags Course

Per Decision 5, the spec assumption "Courses with no tags assigned will require an explicit admin decision on credit generation behaviour" is resolved: **credit generation is implicitly disabled for tagless courses**. This will be documented in `FR-020` update and as a validation rule on `CourseExcusalPolicy`.
