# Implementation Plan: Course Enrollment Email and Session Excusals

**Branch**: `004-enrollment-email-excusals` | **Date**: 2026-04-02 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/004-enrollment-email-excusals/spec.md`

## Summary

Replace the stub email service with a real MailKit-based SMTP implementation configured via `appsettings.json`. Introduce a participant portal with two access modes: (1) a public magic-link email flow ("enter your email → receive link → see all your courses and excusal credits") and (2) course-specific safe links embedded in enrollment/excusal emails. Add session excusal and excusal credit mechanics — including per-course policy (tags, validity windows, generation toggle), tenant settings, staff credit management with full audit logging, and self-service credit redemption by participants.

---

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5.x / React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MediatR 12.x, FluentValidation 12.x, EF Core 10 (Npgsql), MailKit + MimeKit (new), React 19, Mantine v9, TanStack Query v5, react-i18next  
**Storage**: PostgreSQL via EF Core (existing); new tables in `registrations`, `courses`, `tenants` schemas  
**Testing**: dotnet test (xUnit), existing integration test project `Terminar.Api.IntegrationTests`  
**Target Platform**: Linux server (.NET Aspire containerized)  
**Project Type**: Web service (backend) + web application (frontend)  
**Performance Goals**: Safe link page load < 2s; excusal submission < 60s end-to-end; credit operations < 5s  
**Constraints**: No new bounded context module (fit within existing modules); no cookie-based session for participants  
**Scale/Scope**: Per-tenant; hundreds of courses, thousands of registrations

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Domain-Driven Design | ✅ PASS | All new aggregates (`Excusal`, `ExcusalCredit`, `ParticipantMagicLink`, `ExcusalValidityWindow`) follow DDD. Business logic in domain layer. SMTP lives in Infrastructure. |
| II. Multi-Tenancy by Default | ✅ PASS | All new entities carry `TenantId`. Magic link flow is tenant-scoped. Participant portal is tenant-scoped. Global query filters applied to all new DbContext entities. |
| III. Multi-Language First | ✅ PASS | All email subjects/bodies use i18n keys. Frontend participant portal uses react-i18next. New i18n keys added to `en.json` and `cs.json`. |
| IV. Clean Architecture | ✅ PASS | MailKit lives in Infrastructure. `IEmailNotificationService` interface remains in Application (moved from Api stub). Domain events trigger email dispatch via MediatR handlers in Infrastructure. |
| V. Spec-First | ✅ PASS | spec.md approved; this plan derived from it. |

**Post-design re-check**: All new entities and contracts preserve DDD layering. `CourseExcusalPolicy` is an owned entity on `Course` aggregate — no additional bounded context boundary crossed. Cross-module references (Registrations → Courses) are by ID only. ✅

---

## Project Structure

### Documentation (this feature)

```text
specs/004-enrollment-email-excusals/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-contracts.md # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — not created here)
```

### Source Code Changes

```text
src/
  Terminar.Modules.Registrations/
    Domain/
      Excusal.cs                        [NEW] Excusal aggregate
      ExcusalCredit.cs                  [NEW] ExcusalCredit aggregate + audit entries
      ExcusalStatus.cs                  [NEW] enum
      ExcusalCreditStatus.cs            [NEW] enum
      ExcusalCreditActionType.cs        [NEW] enum
      ParticipantMagicLink.cs           [NEW] aggregate
      Registration.cs                   [MODIFY] rename SelfCancellationToken → SafeLinkToken
      Events/
        ExcusalCreated.cs               [NEW]
        ExcusalCreditIssued.cs          [NEW]
        ExcusalCreditRedeemed.cs        [NEW]
        ExcusalCreditCancelled.cs       [NEW]
      Repositories/
        IExcusalRepository.cs           [NEW]
        IExcusalCreditRepository.cs     [NEW]
        IParticipantMagicLinkRepository.cs [NEW]
    Application/
      Commands/
        CreateExcusalCommand.cs         [NEW]
        RedeemExcusalCreditCommand.cs   [NEW]
        UpdateExcusalCreditCommand.cs   [NEW] (staff extend/re-tag)
        SoftDeleteExcusalCreditCommand.cs [NEW] (staff)
        RequestMagicLinkCommand.cs      [NEW]
        RedeemMagicLinkCommand.cs       [NEW]
      Queries/
        GetParticipantPortalQuery.cs    [NEW]
        GetParticipantCourseViewQuery.cs [NEW]
        GetExcusalCreditsQuery.cs       [NEW] (staff)
    Infrastructure/
      RegistrationsDbContext.cs         [MODIFY] add new entity sets + rename column
      Repositories/
        ExcusalRepository.cs            [NEW]
        ExcusalCreditRepository.cs      [NEW]
        ParticipantMagicLinkRepository.cs [NEW]
      Migrations/                       [NEW migration]

  Terminar.Modules.Courses/
    Domain/
      Course.cs                         [MODIFY] add CourseExcusalPolicy owned entity
      CourseExcusalPolicy.cs            [NEW] owned value object / EF owned entity
    Application/
      Commands/
        UpdateCourseExcusalPolicyCommand.cs [NEW]
      Queries/
        GetCourseExcusalPolicyQuery.cs  [NEW]
    Infrastructure/
      CoursesDbContext.cs               [MODIFY] add owned entity config
      Migrations/                       [NEW migration]

  Terminar.Modules.Tenants/
    Domain/
      Tenant.cs                         [MODIFY] add TenantExcusalSettings owned entity
      TenantExcusalSettings.cs          [NEW] owned value object
      ExcusalValidityWindow.cs          [NEW] aggregate
      Repositories/
        IExcusalValidityWindowRepository.cs [NEW]
    Application/
      Commands/
        UpdateTenantExcusalSettingsCommand.cs [NEW]
        CreateExcusalValidityWindowCommand.cs [NEW]
        UpdateExcusalValidityWindowCommand.cs [NEW]
        DeleteExcusalValidityWindowCommand.cs [NEW]
      Queries/
        GetExcusalValidityWindowsQuery.cs [NEW]
        GetTenantExcusalSettingsQuery.cs [NEW]
    Infrastructure/
      TenantsDbContext.cs               [MODIFY] add new entities
      Repositories/
        ExcusalValidityWindowRepository.cs [NEW]
      Migrations/                       [NEW migration]

  Terminar.Api/
    Modules/
      ParticipantsModule.cs             [NEW] public participant endpoints
      ExcusalCreditsModule.cs           [NEW] staff credit management endpoints
      ExcusalSettingsModule.cs          [NEW] tenant settings + validity windows
      CoursesModule.cs                  [MODIFY] add excusal policy endpoints
    Notifications/
      IEmailNotificationService.cs      [MODIFY] add new email method signatures
      SmtpEmailNotificationService.cs   [NEW] MailKit implementation
      EmailTemplates.cs                 [NEW] HTML + text template builders per email type
      ExcusalCreatedEmailHandler.cs     [NEW] domain event handler
      ExcusalCreditRedeemedEmailHandler.cs [NEW]
      RegistrationCancelledEmailHandler.cs [NEW] (unenroll via safe link → participant + staff)
    Infrastructure/
      SmtpSettings.cs                   [NEW] POCO for configuration binding
    Program.cs                          [MODIFY] register SmtpEmailNotificationService, new modules, SmtpSettings

frontend/
  src/
    features/
      participant/
        ParticipantPortalRequestPage.tsx  [NEW] public "enter email" page
        ParticipantPortalPage.tsx         [NEW] participant dashboard (portal token auth)
        ParticipantCourseViewPage.tsx     [NEW] course-specific view (safe link token)
        participantApi.ts                 [NEW] API calls for participant endpoints
        components/
          ExcusalCreditCard.tsx           [NEW]
          SessionExcusalRow.tsx           [NEW]
      settings/
        excusal/
          ExcusalSettingsPage.tsx         [NEW] tenant settings + validity windows
          excusalSettingsApi.ts           [NEW]
      excusal-credits/
        ExcusalCreditsPage.tsx            [NEW] staff credit management list
        excusalCreditsApi.ts              [NEW]
    shared/
      i18n/locales/en.json              [MODIFY] add participant portal + excusal keys
      shared/i18n/locales/cs.json       [MODIFY] add Czech translations
    app/
      router.tsx                        [MODIFY] add new public + staff routes
```

---

## Complexity Tracking

No constitution violations. All new entities fit within existing module boundaries.

---

## Implementation Phases (for /speckit.tasks input)

### Phase A — Infrastructure Foundation (no user-visible change)

1. **A1** — Add MailKit + MimeKit packages; implement `SmtpSettings`, `SmtpEmailNotificationService`, `EmailTemplates`; replace stub in DI. Extend `IEmailNotificationService` with new method signatures.
2. **A2** — EF migration: rename `SelfCancellationToken` → `SafeLinkToken` in Registrations.
3. **A3** — Tenant module: `TenantExcusalSettings` owned entity + `ExcusalValidityWindow` aggregate + migration.
4. **A4** — Courses module: `CourseExcusalPolicy` owned entity on `Course` + migration.

### Phase B — Backend: Excusal Core

5. **B1** — Registrations module domain: `Excusal` aggregate + `ExcusalCreated` domain event + repository interface.
6. **B2** — `CreateExcusalCommand` handler: deadline validation (hours-before-session from TenantExcusalSettings / CourseExcusalPolicy), duplicate check, persist, emit event.
7. **B3** — `ExcusalCredit` aggregate + repository + audit entries.
8. **B4** — `ExcusalCreatedEmailHandler`: on `ExcusalCreated` event, issue credit if policy enabled, emit `ExcusalCreditIssued` event.

### Phase C — Backend: Participant Portal

9. **C1** — `ParticipantMagicLink` aggregate + repository + `RequestMagicLinkCommand` + `MagicLinkRequest` email.
10. **C2** — `RedeemMagicLinkCommand`: validate token, issue portal token.
11. **C3** — `GetParticipantPortalQuery`: by portal token → all enrollments + credits.
12. **C4** — `GetParticipantCourseViewQuery`: by safe link token → single enrollment view.
13. **C5** — `ParticipantsModule` minimal API wiring (all public endpoints).

### Phase D — Backend: Staff Management

14. **D1** — `UpdateExcusalCreditCommand` (extend/re-tag) + `SoftDeleteExcusalCreditCommand` + `GetExcusalCreditsQuery`.
15. **D2** — `ExcusalCreditsModule` minimal API wiring.
16. **D3** — Tenant settings commands/queries + `ExcusalSettingsModule` minimal API wiring.
17. **D4** — Course excusal policy command/query + endpoint in `CoursesModule`.

### Phase E — Frontend: Participant Portal

18. **E1** — `participantApi.ts` (all public participant endpoints).
19. **E2** — `ParticipantPortalRequestPage` (email input + 202 handling).
20. **E3** — `ParticipantCourseViewPage` (course view via safe link token, excuse/unenroll actions).
21. **E4** — `ParticipantPortalPage` (portal token from localStorage, all enrollments + credits, redemption flow).
22. **E5** — i18n keys (en + cs) for participant portal.
23. **E6** — Router update: new public routes.

### Phase F — Frontend: Staff Features

24. **F1** — `ExcusalCreditsPage` + `excusalCreditsApi.ts` (list, extend, re-tag, soft-delete).
25. **F2** — `ExcusalSettingsPage` + `excusalSettingsApi.ts` (tenant settings + validity windows CRUD).
26. **F3** — `CourseExcusalPolicyPage` per course (linked from course detail).
27. **F4** — i18n keys (en + cs) for staff excusal management.
28. **F5** — Router + nav update: staff routes.

### Phase G — Email Completion

29. **G1** — `RegistrationCancelledEmailHandler`: participant confirmation + staff notification on safe-link unenroll.
30. **G2** — `ExcusalCreditRedeemedEmailHandler`: participant redemption confirmation.
31. **G3** — Enrollment confirmation email update: include direct course safe link URL.

### Phase H — Testing

32. **H1** — Unit tests: `Excusal` domain invariants, `ExcusalCredit` state transitions + audit, `ParticipantMagicLink` token lifecycle.
33. **H2** — Unit tests: deadline calculation logic (unenrollment window, excusal window).
34. **H3** — Integration tests: participant portal endpoints (magic link → redeem → portal view → excuse → credit).
35. **H4** — Integration tests: staff credit management (extend, re-tag, soft-delete, audit log).
36. **H5** — Integration tests: SMTP settings + validity window CRUD.
