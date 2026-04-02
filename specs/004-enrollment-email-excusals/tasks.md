# Tasks: Course Enrollment Email and Session Excusals

**Input**: Design documents from `/specs/004-enrollment-email-excusals/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-contracts.md ✅

**Organization**: Tasks grouped by user story for independent implementation and delivery.  
**Tests**: Not requested in spec — test tasks omitted.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in same phase)
- **[Story]**: Maps to user story from spec.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add new dependencies and configure SMTP infrastructure foundation.

- [x] T001 Add MailKit and MimeKit NuGet packages to `src/Terminar.Api/Terminar.Api.csproj`
- [x] T002 [P] Create `SmtpSettings` POCO in `src/Terminar.Api/Infrastructure/SmtpSettings.cs` with fields: Host, Port, Username, Password, FromAddress, FromName, UseSsl, UseStartTls
- [x] T003 [P] Add SMTP config section to `src/Terminar.Api/appsettings.json` and `src/Terminar.Api/appsettings.Development.json` (Development uses localhost:1025 for MailHog)
- [x] T004 Register `builder.Services.Configure<SmtpSettings>(...)` in `src/Terminar.Api/Program.cs` (depends on T002)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain model changes and database structure that ALL user stories require.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Registrations Module — Registration Field Rename

- [x] T005 Rename `SelfCancellationToken` → `SafeLinkToken` in `src/Terminar.Modules.Registrations/Domain/Registration.cs` (property rename + factory method + Cancel method update)
- [x] T006 Update `RegistrationsDbContext` column mapping from `self_cancellation_token` → `safe_link_token` in `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsDbContext.cs`
- [x] T007 Update `CancelRegistrationCommand` + handler in `src/Terminar.Modules.Registrations/Application/Commands/CancelRegistration/` to reference `SafeLinkToken`
- [x] T008 Update `RegistrationsModule.cs` cancellation endpoint to read `SafeLinkToken` from `src/Terminar.Api/Modules/RegistrationsModule.cs`
- [x] T009 Add EF migration `RenameSelfCancellationTokenToSafeLinkToken` for Registrations module: `dotnet ef migrations add RenameSelfCancellationTokenToSafeLinkToken --project src/Terminar.Modules.Registrations --startup-project src/Terminar.Api --context RegistrationsDbContext`

### Tenants Module — Excusal Settings & Validity Windows

- [x] T010 [P] Create `TenantExcusalSettings` owned entity class in `src/Terminar.Modules.Tenants/Domain/TenantExcusalSettings.cs` with fields: CreditGenerationEnabled (default false), ForwardWindowCount (default 2), UnenrollmentDeadlineDays (default 14), ExcusalDeadlineHours (default 24)
- [x] T011 [P] Create `ExcusalValidityWindow` aggregate in `src/Terminar.Modules.Tenants/Domain/ExcusalValidityWindow.cs` with fields: TenantId, Name, StartDate, EndDate, CreatedAt, DeletedAt; factory method `Create()`; invariant: EndDate > StartDate, Name unique per tenant
- [x] T012 [P] Create `IExcusalValidityWindowRepository` interface in `src/Terminar.Modules.Tenants/Domain/Repositories/IExcusalValidityWindowRepository.cs` with: GetByIdAsync, ListByTenantAsync, AddAsync, UpdateAsync
- [x] T013 Add `TenantExcusalSettings` as EF Core owned entity on `Tenant` and `ExcusalValidityWindow` entity set in `src/Terminar.Modules.Tenants/Infrastructure/TenantsDbContext.cs` (depends on T010, T011)
- [x] T014 Add EF migration `TenantExcusalSettingsAndWindows` for Tenants module (depends on T013)
- [x] T015 Implement `ExcusalValidityWindowRepository` in `src/Terminar.Modules.Tenants/Infrastructure/Repositories/ExcusalValidityWindowRepository.cs` (depends on T013)

### Courses Module — Course Excusal Policy

- [x] T016 [P] Create `CourseExcusalPolicy` owned entity in `src/Terminar.Modules.Courses/Domain/CourseExcusalPolicy.cs` with fields: CreditGenerationOverride (bool?), ValidityWindowId (Guid?), Tags (List<string>); static helper `EffectiveCreditGenerationEnabled(bool tenantDefault)`
- [x] T017 [P] Add `ExcusalPolicy` owned property to `Course` aggregate in `src/Terminar.Modules.Courses/Domain/Course.cs`; initialize with defaults in `Create()` factory
- [x] T018 Configure `CourseExcusalPolicy` as EF Core owned entity on `Course` in `src/Terminar.Modules.Courses/Infrastructure/CoursesDbContext.cs`; map `Tags` as PostgreSQL text array (depends on T016, T017)
- [x] T019 Add EF migration `CourseExcusalPolicy` for Courses module (depends on T018)

### Participant Magic Link Infrastructure

- [x] T020 Create `ParticipantMagicLink` aggregate in `src/Terminar.Modules.Registrations/Domain/ParticipantMagicLink.cs` with fields: TenantId, ParticipantEmail, MagicLinkToken (string), MagicLinkExpiresAt, MagicLinkUsedAt?, PortalToken (string?), PortalTokenExpiresAt?; methods: `Create()` (generates 32-byte URL-safe token, 15-min expiry), `Redeem()` (sets UsedAt, generates portal token with 7-day expiry)
- [x] T021 Create `IParticipantMagicLinkRepository` in `src/Terminar.Modules.Registrations/Domain/Repositories/IParticipantMagicLinkRepository.cs` with: GetByMagicLinkTokenAsync, GetByPortalTokenAsync, AddAsync, UpdateAsync
- [x] T022 Add `ParticipantMagicLink` entity set to `RegistrationsDbContext` in `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsDbContext.cs`; add unique indexes on (TenantId, MagicLinkToken) and (TenantId, PortalToken) (depends on T020)
- [x] T023 Add EF migration `AddParticipantMagicLink` for Registrations module (depends on T009, T022)
- [x] T024 Implement `ParticipantMagicLinkRepository` in `src/Terminar.Modules.Registrations/Infrastructure/Repositories/ParticipantMagicLinkRepository.cs` (depends on T022)
- [x] T025 Implement `RequestMagicLinkCommand` + handler in `src/Terminar.Modules.Registrations/Application/Commands/RequestMagicLink/RequestMagicLinkCommand.cs` + `RequestMagicLinkHandler.cs`: validate email has active registrations in tenant, create ParticipantMagicLink, dispatch to email service (depends on T020, T024)
- [x] T026 Implement `RedeemMagicLinkCommand` + handler in `src/Terminar.Modules.Registrations/Application/Commands/RedeemMagicLink/RedeemMagicLinkCommand.cs` + `RedeemMagicLinkHandler.cs`: validate token not expired/used, call Redeem(), return portal token (depends on T020, T024)
- [x] T027 [P] Add skeleton i18n keys for participant portal, excusal, and settings features to `frontend/src/shared/i18n/locales/en.json` and `frontend/src/shared/i18n/locales/cs.json`

**Checkpoint**: Foundation complete — all user story phases can now begin.

---

## Phase 3: User Story 1 — Enrollment Confirmation Email (Priority: P1) 🎯 MVP

**Goal**: Participant automatically receives a real email with a safe link when enrolled in a course.

**Independent Test**: Enroll a participant via staff API; verify email arrives in MailHog at localhost:8025 containing the course name, sessions list, and a working `/participant/course/{safeLinkToken}` link.

- [x] T028 [US1] Expand `IEmailNotificationService` interface in `src/Terminar.Api/Notifications/IEmailNotificationService.cs` with new method signatures: `SendEnrollmentConfirmationAsync`, `SendMagicLinkAsync`, `SendExcusalConfirmationAsync`, `SendUnenrollmentConfirmationAsync`, `SendStaffUnenrollmentNotificationAsync`, `SendCreditRedemptionConfirmationAsync`
- [x] T029 [US1] Implement `EmailTemplates` static class in `src/Terminar.Api/Notifications/EmailTemplates.cs` with `BuildEnrollmentConfirmation(participantName, courseTitle, sessions, safeLinkUrl)` method returning `(string subject, string htmlBody, string textBody)`
- [x] T030 [US1] Implement `SmtpEmailNotificationService` in `src/Terminar.Api/Notifications/SmtpEmailNotificationService.cs` using MailKit `SmtpClient`; bind to `SmtpSettings` from DI; implement `SendEnrollmentConfirmationAsync` first (stub remaining methods with log output) (depends on T001, T002, T004, T028, T029)
- [x] T031 [US1] Register `SmtpEmailNotificationService` as `IEmailNotificationService` in `src/Terminar.Api/Program.cs`, replacing `StubEmailNotificationService` (depends on T030)
- [x] T032 [US1] Update `RegistrationCreatedEmailHandler` in `src/Terminar.Api/Notifications/RegistrationCreatedEmailHandler.cs` to: query full course details (title + sessions) via MediatR, build safe link URL from `Registration.SafeLinkToken`, call `SendEnrollmentConfirmationAsync` (depends on T005, T030)

**Checkpoint**: US1 fully functional — real enrollment emails delivered with safe link.

---

## Phase 4: User Story 2 — Public Session View via Safe Link (Priority: P2)

**Goal**: Participant follows safe link from email and sees their course sessions read-only, without login.

**Independent Test**: Access `GET /api/v1/participants/courses/{safeLinkToken}` with a valid token; verify response contains course name, sessions with dates/times, and enrollment status.

- [x] T033 [US2] Implement `GetParticipantCourseViewQuery` + handler in `src/Terminar.Modules.Registrations/Application/Queries/GetParticipantCourseView/GetParticipantCourseViewQuery.cs` + `GetParticipantCourseViewHandler.cs`: look up Registration by SafeLinkToken, load course + sessions via cross-module read (by CourseId), return `ParticipantCourseViewDto` including sessions with excusal deadlines
- [x] T034 [US2] Create `ParticipantsModule.cs` in `src/Terminar.Api/Modules/ParticipantsModule.cs`; add `GET /api/v1/participants/courses/{safeLinkToken}` endpoint (AllowAnonymous, tenant from header) delegating to `GetParticipantCourseViewQuery` (depends on T033)
- [x] T035 [P] [US2] Create `participantApi.ts` in `frontend/src/features/participant/participantApi.ts` with `getCourseView(safeLinkToken)` function (uses `fetch` directly, no auth header)
- [x] T036 [P] [US2] Create `ParticipantCourseViewPage.tsx` in `frontend/src/features/participant/ParticipantCourseViewPage.tsx`: read-only page showing course title, session list (date/time/location/status), enrollment status; uses `safeLinkToken` from URL param (depends on T035)
- [x] T037 [US2] Add `/participant/course/:safeLinkToken` route (public, no AuthGuard) to `frontend/src/app/router.tsx` (depends on T036)
- [x] T038 [P] [US2] Add i18n translation keys for course view page to `frontend/src/shared/i18n/locales/en.json` and `cs.json`

**Checkpoint**: US2 functional — safe link opens participant course view in browser.

---

## Phase 5: User Story 3 — Unenroll from Entire Course (Priority: P3)

**Goal**: Participant can self-unenroll via safe link within the configured deadline.

**Independent Test**: Call `POST /api/v1/participants/courses/{safeLinkToken}/unenroll` within the unenrollment window; verify registration status changes to Cancelled, participant receives confirmation email, staff receives notification email.

- [x] T039 [US3] Add `ICourseSessionReader` port interface in `src/Terminar.Modules.Registrations/Application/Ports/ICourseSessionReader.cs` with `GetFirstSessionDateAsync(courseId)` (enables deadline calculation without shared DbContext); implement in `src/Terminar.Modules.Registrations/Infrastructure/Ports/CourseSessionReader.cs` reading from CoursesDbContext
- [x] T040 [US3] Add unenrollment deadline validation to `CancelRegistrationCommand` handler in `src/Terminar.Modules.Registrations/Application/Commands/CancelRegistration/CancelRegistrationHandler.cs`: read `TenantExcusalSettings.UnenrollmentDeadlineDays` via query, compare against first session date; return 422 if deadline passed
- [x] T041 [US3] Add `POST /api/v1/participants/courses/{safeLinkToken}/unenroll` endpoint to `src/Terminar.Api/Modules/ParticipantsModule.cs`: look up Registration by SafeLinkToken, dispatch CancelRegistrationCommand (depends on T040)
- [x] T042 [US3] Add `BuildUnenrollmentConfirmation` and `BuildStaffUnenrollmentNotification` template methods to `src/Terminar.Api/Notifications/EmailTemplates.cs`
- [x] T043 [US3] Implement `SendUnenrollmentConfirmationAsync` and `SendStaffUnenrollmentNotificationAsync` on `SmtpEmailNotificationService` in `src/Terminar.Api/Notifications/SmtpEmailNotificationService.cs` (depends on T030, T042)
- [x] T044 [US3] Create `RegistrationCancelledEmailHandler` in `src/Terminar.Api/Notifications/RegistrationCancelledEmailHandler.cs` as `INotificationHandler<RegistrationCancelled>`; send participant confirmation + staff notification (depends on T043)
- [x] T045 [US3] Add unenroll button + deadline display to `frontend/src/features/participant/ParticipantCourseViewPage.tsx`: show "Unenroll from course" button when `canUnenroll=true`; show deadline message when `canUnenroll=false` (depends on T036)
- [x] T046 [P] [US3] Add `unenroll(safeLinkToken)` function to `frontend/src/features/participant/participantApi.ts`
- [x] T047 [P] [US3] Add i18n keys for unenroll flow to `frontend/src/shared/i18n/locales/en.json` and `cs.json`

**Checkpoint**: US3 functional — participant can unenroll via safe link within deadline; emails sent.

---

## Phase 6: User Story 4 — Excuse from Individual Session (Priority: P4)

**Goal**: Participant can excuse from a specific session via safe link; absence is recorded; optional credit issued.

**Independent Test**: Call `POST /api/v1/participants/courses/{safeLinkToken}/sessions/{sessionId}/excuse` within excusal window; verify `Excusal` record created with `status=Recorded`; if course has credit generation enabled, verify `ExcusalCredit` record also created.

### Domain Model

- [x] T048 [US4] Create `ExcusalStatus` enum in `src/Terminar.Modules.Registrations/Domain/ExcusalStatus.cs`: `Recorded | CreditIssued`
- [x] T049 [US4] Create `ExcusalCreated` domain event in `src/Terminar.Modules.Registrations/Domain/Events/ExcusalCreated.cs` with fields: ExcusalId, RegistrationId, CourseId, SessionId, TenantId, OccurredAt
- [x] T050 [US4] Implement `Excusal` aggregate in `src/Terminar.Modules.Registrations/Domain/Excusal.cs`: fields per data-model.md; factory `Create(tenantId, registrationId, courseId, sessionId, email, name)`; method `MarkCreditIssued(creditId)`; raises `ExcusalCreated` event
- [x] T051 [US4] Create `IExcusalRepository` interface in `src/Terminar.Modules.Registrations/Domain/Repositories/IExcusalRepository.cs` with: GetByIdAsync, ExistsForSessionAsync (duplicate check), AddAsync, UpdateAsync, ListByCourseAsync
- [x] T052 [US4] Add `Excusal` entity set to `RegistrationsDbContext` with unique index on (RegistrationId, SessionId) in `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsDbContext.cs`
- [x] T053 [US4] Add EF migration `AddExcusal` for Registrations module (depends on T052)
- [x] T054 [US4] Implement `ExcusalRepository` in `src/Terminar.Modules.Registrations/Infrastructure/Repositories/ExcusalRepository.cs` (depends on T052)

### Application Layer

- [x] T055 [US4] Create `IExcusalValidityWindowReader` port in `src/Terminar.Modules.Registrations/Application/Ports/IExcusalValidityWindowReader.cs` with `GetWindowsFromAsync(windowId, forwardCount)`; implement in `src/Terminar.Modules.Registrations/Infrastructure/Ports/ExcusalValidityWindowReader.cs` reading from TenantsDbContext
- [x] T056 [US4] Create `ICourseExcusalPolicyReader` port in `src/Terminar.Modules.Registrations/Application/Ports/ICourseExcusalPolicyReader.cs` with `GetPolicyAsync(courseId)`; implement in `src/Terminar.Modules.Registrations/Infrastructure/Ports/CourseExcusalPolicyReader.cs` reading from CoursesDbContext
- [x] T057 [US4] Implement `CreateExcusalCommand` + `CreateExcusalHandler` + `CreateExcusalValidator` in `src/Terminar.Modules.Registrations/Application/Commands/CreateExcusal/`: validate excusal deadline (hours-before-session from TenantExcusalSettings), duplicate check, persist Excusal, return `{ excusalId, creditIssued: false }` (depends on T050, T054, T055, T056)

### API & Email

- [x] T058 [US4] Add `POST /api/v1/participants/courses/{safeLinkToken}/sessions/{sessionId}/excuse` endpoint to `src/Terminar.Api/Modules/ParticipantsModule.cs`: resolve registration by SafeLinkToken, dispatch `CreateExcusalCommand` (depends on T057)
- [x] T059 [US4] Add `BuildExcusalConfirmation` template method to `src/Terminar.Api/Notifications/EmailTemplates.cs`
- [x] T060 [US4] Implement `SendExcusalConfirmationAsync` on `SmtpEmailNotificationService` in `src/Terminar.Api/Notifications/SmtpEmailNotificationService.cs`
- [x] T061 [US4] Create `ExcusalCreatedEmailHandler` in `src/Terminar.Api/Notifications/ExcusalCreatedEmailHandler.cs` as `INotificationHandler<ExcusalCreated>`; send excusal confirmation email to participant (depends on T060)

### Frontend

- [x] T062 [P] [US4] Add `excuseFromSession(safeLinkToken, sessionId)` function to `frontend/src/features/participant/participantApi.ts`
- [x] T063 [US4] Add per-session excuse button + deadline display to `frontend/src/features/participant/ParticipantCourseViewPage.tsx`: show "Excuse me" button when session excusal deadline not passed; show "Deadline passed" when expired; update session state after successful excusal (depends on T036, T062)
- [x] T064 [P] [US4] Add i18n keys for session excusal flow to `frontend/src/shared/i18n/locales/en.json` and `cs.json`

**Checkpoint**: US4 functional — participant can excuse from sessions; absence record created; confirmation email sent.

---

## Phase 7: User Story 5 — Excusal Credit Redemption (Priority: P4)

**Goal**: Participant with a valid excusal credit can redeem it for a makeup enrollment via the portal.

**Independent Test**: Issue credit for a tagged course; request magic link; redeem magic link for portal token; call `POST /api/v1/participants/credits/{id}/redeem` with a matching course; verify new Registration created and credit status = Redeemed.

### Domain Model

- [x] T065 [US5] Create `ExcusalCreditStatus` enum in `src/Terminar.Modules.Registrations/Domain/ExcusalCreditStatus.cs`: `Active | Redeemed | Expired | Cancelled`
- [x] T066 [US5] Create `ExcusalCreditActionType` enum in `src/Terminar.Modules.Registrations/Domain/ExcusalCreditActionType.cs`: `Extend | ReTag | SoftDelete`
- [x] T067 [US5] Create domain events in `src/Terminar.Modules.Registrations/Domain/Events/`: `ExcusalCreditIssued.cs`, `ExcusalCreditRedeemed.cs`, `ExcusalCreditCancelled.cs`
- [x] T068 [US5] Implement `ExcusalCreditAuditEntry` entity in `src/Terminar.Modules.Registrations/Domain/ExcusalCredit.cs` (inner class or same file): fields: Id, ExcusalCreditId, ActorStaffId, ActionType, FieldChanged, PreviousValue, NewValue, Timestamp; append-only
- [x] T069 [US5] Implement `ExcusalCredit` aggregate in `src/Terminar.Modules.Registrations/Domain/ExcusalCredit.cs`: all fields from data-model.md; factory `Issue(excusal, tags, validWindowIds)`; methods: `Redeem(courseId)`, `Extend(additionalWindowIds, actorStaffId)`, `ReTag(newTags, actorStaffId)`, `SoftDelete(actorStaffId)`; owned collection `AuditEntries`; all mutations validate `Status == Active`; raises domain events
- [x] T070 [US5] Create `IExcusalCreditRepository` in `src/Terminar.Modules.Registrations/Domain/Repositories/IExcusalCreditRepository.cs` with: GetByIdAsync, ListByParticipantEmailAsync, ListByTenantAsync (paged), AddAsync, UpdateAsync
- [x] T071 [US5] Add `ExcusalCredit` and `ExcusalCreditAuditEntry` entity sets to `RegistrationsDbContext` in `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsDbContext.cs`; configure `Tags` as text array, `ValidWindowIds` as uuid array, `AuditEntries` as owned collection (depends on T068, T069)
- [x] T072 [US5] Add EF migration `AddExcusalCredit` for Registrations module (depends on T053, T071)
- [x] T073 [US5] Implement `ExcusalCreditRepository` in `src/Terminar.Modules.Registrations/Infrastructure/Repositories/ExcusalCreditRepository.cs` (depends on T071)

### Credit Issuance

- [x] T074 [US5] Create `ExcusalCreditIssuanceService` domain service in `src/Terminar.Modules.Registrations/Domain/Services/ExcusalCreditIssuanceService.cs`: given `Excusal` + `CourseExcusalPolicy` + tenant settings + validity windows, determines if credit should be issued and calls `ExcusalCredit.Issue()`; validates tags non-empty and window assigned
- [x] T075 [US5] Create `ExcusalCreatedCreditHandler` in `src/Terminar.Api/Notifications/ExcusalCreatedCreditHandler.cs` as `INotificationHandler<ExcusalCreated>`: reads course policy via `ICourseExcusalPolicyReader`, reads tenant settings, calls `ExcusalCreditIssuanceService`, persists credit, calls `Excusal.MarkCreditIssued()`, raises `ExcusalCreditIssued` event (depends on T057, T069, T073, T074)

### Redemption

- [x] T076 [US5] Implement `RedeemExcusalCreditCommand` + `RedeemExcusalCreditHandler` + `RedeemExcusalCreditValidator` in `src/Terminar.Modules.Registrations/Application/Commands/RedeemExcusalCredit/`: validate portal token, load credit, verify Active status, check tag intersection with target course, check target course not at capacity, create Registration for target course, call `credit.Redeem()`, persist (depends on T069, T073)

### Portal Queries

- [x] T077 [US5] Implement `GetParticipantPortalQuery` + `GetParticipantPortalHandler` in `src/Terminar.Modules.Registrations/Application/Queries/GetParticipantPortal/`: validate portal token via `IParticipantMagicLinkRepository`, load all active Registrations by email+tenant, load associated credits; return `ParticipantPortalDto` (depends on T024, T073)

### API Endpoints

- [x] T078 [US5] Add to `src/Terminar.Api/Modules/ParticipantsModule.cs`:
  - `POST /api/v1/participants/magic-link` → `RequestMagicLinkCommand` (depends on T025)
  - `POST /api/v1/participants/portal/redeem` → `RedeemMagicLinkCommand` (depends on T026)
  - `GET /api/v1/participants/portal` with `X-Portal-Token` header → `GetParticipantPortalQuery` (depends on T077)
  - `POST /api/v1/participants/credits/{creditId}/redeem` → `RedeemExcusalCreditCommand` (depends on T076)

### Email

- [x] T079 [US5] Add `BuildCreditRedemptionConfirmation` template method to `src/Terminar.Api/Notifications/EmailTemplates.cs`
- [x] T080 [US5] Implement `SendCreditRedemptionConfirmationAsync` on `SmtpEmailNotificationService` in `src/Terminar.Api/Notifications/SmtpEmailNotificationService.cs` (depends on T030)
- [x] T081 [US5] Create `ExcusalCreditRedeemedEmailHandler` in `src/Terminar.Api/Notifications/ExcusalCreditRedeemedEmailHandler.cs` as `INotificationHandler<ExcusalCreditRedeemed>` (depends on T080)

### Magic Link Email

- [x] T082 [US5] Add `BuildMagicLinkEmail` template method to `src/Terminar.Api/Notifications/EmailTemplates.cs`
- [x] T083 [US5] Implement `SendMagicLinkAsync` on `SmtpEmailNotificationService` in `src/Terminar.Api/Notifications/SmtpEmailNotificationService.cs`; update `RequestMagicLinkHandler` to send email via `IEmailNotificationService` (depends on T030, T082)

### Frontend

- [x] T084 [US5] Create `ParticipantPortalRequestPage.tsx` in `frontend/src/features/participant/ParticipantPortalRequestPage.tsx`: text input for email, submit triggers `POST /api/v1/participants/magic-link`, shows success message ("check your email")
- [x] T085 [US5] Add `requestMagicLink(email)` and `redeemMagicLink(token)` and `getPortal()` and `redeemCredit(creditId, targetCourseId)` to `frontend/src/features/participant/participantApi.ts`
- [x] T086 [US5] Create `ExcusalCreditCard.tsx` component in `frontend/src/features/participant/components/ExcusalCreditCard.tsx`: displays credit tags, valid-until, status; "Redeem" button opens modal
- [x] T087 [US5] Create `RedeemCreditModal.tsx` in `frontend/src/features/participant/components/RedeemCreditModal.tsx`: search/select target course by tag, confirm redemption action
- [x] T088 [US5] Create `ParticipantPortalPage.tsx` in `frontend/src/features/participant/ParticipantPortalPage.tsx`: reads `portalToken` from localStorage (set after magic link redemption), displays all enrollments as cards (linking to `/participant/course/:token`) and excusal credits via `ExcusalCreditCard` (depends on T085, T086, T087)
- [x] T089 [US5] Add `/participant` and `/participant/portal` routes (both public, no AuthGuard) to `frontend/src/app/router.tsx`; store portal token to localStorage in magic link redemption callback (depends on T084, T088)
- [x] T090 [P] [US5] Add i18n keys for portal, magic link, and credit redemption to `frontend/src/shared/i18n/locales/en.json` and `cs.json`

**Checkpoint**: US5 functional — participant can request portal access, view all enrollments + credits, and redeem credits for makeup enrollments.

---

## Phase 8: User Story 6 — Staff Excusal Credit Management (Priority: P4)

**Goal**: Staff can view, prolong, re-tag, and soft-delete excusal credits with full audit logging.

**Independent Test**: Call `PATCH /api/v1/excusal-credits/{id}` with new tags and additional window IDs; verify credit updated and audit entry created. Call `DELETE /api/v1/excusal-credits/{id}`; verify credit status = Cancelled, `DeletedAt` set, participant sees "Cancelled by organizer" on safe link page.

- [x] T091 [US6] Implement `UpdateExcusalCreditCommand` + `UpdateExcusalCreditHandler` + `UpdateExcusalCreditValidator` in `src/Terminar.Modules.Registrations/Application/Commands/UpdateExcusalCredit/`: accept `additionalWindowIds` (optional) and `tags` (optional, must be non-empty if provided); call `credit.Extend()` and/or `credit.ReTag()` as needed; persist; return updated credit (depends on T069, T073)
- [x] T092 [US6] Implement `SoftDeleteExcusalCreditCommand` + `SoftDeleteExcusalCreditHandler` in `src/Terminar.Modules.Registrations/Application/Commands/SoftDeleteExcusalCredit/`: load credit, call `credit.SoftDelete(actorStaffId)`, persist; extract staff ID from JWT claim (depends on T069, T073)
- [x] T093 [US6] Implement `GetExcusalCreditsQuery` + `GetExcusalCreditsHandler` in `src/Terminar.Modules.Registrations/Application/Queries/GetExcusalCredits/`: paged list filtered by TenantId, optional status and email filters; include audit entries (depends on T073)
- [x] T094 [US6] Create `ExcusalCreditsModule.cs` in `src/Terminar.Api/Modules/ExcusalCreditsModule.cs` with three endpoints (StaffOrAdmin policy):
  - `GET /api/v1/excusal-credits` → `GetExcusalCreditsQuery`
  - `PATCH /api/v1/excusal-credits/{id}` → `UpdateExcusalCreditCommand`
  - `DELETE /api/v1/excusal-credits/{id}` → `SoftDeleteExcusalCreditCommand`
  Register module in `src/Terminar.Api/Program.cs` (depends on T091, T092, T093)
- [x] T095 [P] [US6] Create `excusalCreditsApi.ts` in `frontend/src/features/excusal-credits/excusalCreditsApi.ts` with: `listCredits`, `updateCredit`, `deleteCredit` functions
- [x] T096 [P] [US6] Create `CreditEditModal.tsx` in `frontend/src/features/excusal-credits/components/CreditEditModal.tsx`: form for adding validity windows (multi-select from tenant windows) and replacing tags
- [x] T097 [US6] Create `ExcusalCreditsPage.tsx` in `frontend/src/features/excusal-credits/ExcusalCreditsPage.tsx`: paginated table of credits with status badges, expand row to show audit log, Edit + Delete action buttons (depends on T095, T096)
- [x] T098 [US6] Add `/app/excusal-credits` route to `frontend/src/app/router.tsx` and nav link to `AppShellLayout.tsx` (Staff-visible) (depends on T097)
- [x] T099 [P] [US6] Add i18n keys for staff credit management to `frontend/src/shared/i18n/locales/en.json` and `cs.json`

**Checkpoint**: US6 functional — staff can manage excusal credits with full audit trail.

---

## Phase 9: User Story 7 — Tenant Settings and Excusal Policy (Priority: P5)

**Goal**: Admins can configure excusal deadlines, validity windows, global credit generation toggle, and per-course policy overrides.

**Independent Test**: Set `POST /api/v1/settings/excusal-windows` to create a window, then `PATCH /api/v1/settings/excusal-policy` to enable credit generation; create a course, set policy via `PATCH /api/v1/courses/{id}/excusal-policy` with tags and window; enroll and excuse a participant — verify credit issued with correct tags and window range.

### Tenants Module — Commands & Queries

- [x] T100 [US7] Implement `UpdateTenantExcusalSettingsCommand` + handler in `src/Terminar.Modules.Tenants/Application/Commands/UpdateTenantExcusalSettings/`: updates TenantExcusalSettings owned entity on Tenant; validates forwardWindowCount >= 1 (depends on T013, T015)
- [x] T101 [P] [US7] Implement `CreateExcusalValidityWindowCommand` + handler in `src/Terminar.Modules.Tenants/Application/Commands/CreateExcusalValidityWindow/`: creates ExcusalValidityWindow, validates date range and name uniqueness (depends on T015)
- [x] T102 [P] [US7] Implement `UpdateExcusalValidityWindowCommand` + handler in `src/Terminar.Modules.Tenants/Application/Commands/UpdateExcusalValidityWindow/` (depends on T015)
- [x] T103 [P] [US7] Implement `DeleteExcusalValidityWindowCommand` + handler in `src/Terminar.Modules.Tenants/Application/Commands/DeleteExcusalValidityWindow/`: soft-deletes window; 409 if referenced by active credits (cross-module check via port) (depends on T015)
- [x] T104 [P] [US7] Implement `GetTenantExcusalSettingsQuery` + handler in `src/Terminar.Modules.Tenants/Application/Queries/GetTenantExcusalSettings/` (depends on T013)
- [x] T105 [P] [US7] Implement `ListExcusalValidityWindowsQuery` + handler in `src/Terminar.Modules.Tenants/Application/Queries/ListExcusalValidityWindows/` (depends on T015)

### Courses Module — Course Excusal Policy

- [x] T106 [P] [US7] Implement `UpdateCourseExcusalPolicyCommand` + `UpdateCourseExcusalPolicyHandler` + `UpdateCourseExcusalPolicyValidator` in `src/Terminar.Modules.Courses/Application/Commands/UpdateCourseExcusalPolicy/`: update owned `ExcusalPolicy` on Course; validate ValidityWindowId exists in tenant via port (depends on T016, T017, T019)
- [x] T107 [P] [US7] Implement `GetCourseExcusalPolicyQuery` + handler in `src/Terminar.Modules.Courses/Application/Queries/GetCourseExcusalPolicy/`: returns policy + computed `effectiveCreditGenerationEnabled` (depends on T016, T017)

### API Modules

- [x] T108 [US7] Create `ExcusalSettingsModule.cs` in `src/Terminar.Api/Modules/ExcusalSettingsModule.cs` with endpoints (StaffOrAdmin for GET, AdminOnly for mutations):
  - `GET/POST/PATCH/DELETE /api/v1/settings/excusal-windows`
  - `GET/PATCH /api/v1/settings/excusal-policy`
  Register in `src/Terminar.Api/Program.cs` (depends on T100, T101, T102, T103, T104, T105)
- [x] T109 [US7] Add `GET /api/v1/courses/{courseId}/excusal-policy` and `PATCH /api/v1/courses/{courseId}/excusal-policy` endpoints to `src/Terminar.Api/Modules/CoursesModule.cs` (depends on T106, T107)

### Frontend

- [x] T110 [P] [US7] Create `excusalSettingsApi.ts` in `frontend/src/features/settings/excusal/excusalSettingsApi.ts` with functions for all tenant settings and validity window endpoints
- [x] T111 [P] [US7] Create `ValidityWindowsManagerPage.tsx` in `frontend/src/features/settings/excusal/ValidityWindowsManagerPage.tsx`: CRUD table for validity windows (create/edit/delete with confirmation)
- [x] T112 [P] [US7] Create `ExcusalSettingsPage.tsx` in `frontend/src/features/settings/excusal/ExcusalSettingsPage.tsx`: form for global credit generation toggle, forward window count, unenrollment deadline days, excusal deadline hours; embeds `ValidityWindowsManagerPage` as a section
- [x] T113 [US7] Create `CourseExcusalPolicySection.tsx` component in `frontend/src/features/courses/components/CourseExcusalPolicySection.tsx`: inline form for per-course override toggle, window selector (from tenant windows list), tag input; embed in `CourseDetailPage.tsx`
- [x] T114 [US7] Add `/app/settings/excusal` route (AdminOnly) to `frontend/src/app/router.tsx` and nav link to `AppShellLayout.tsx` (depends on T112)
- [x] T115 [P] [US7] Add i18n keys for excusal settings and course policy to `frontend/src/shared/i18n/locales/en.json` and `cs.json`

**Checkpoint**: US7 functional — admins can configure all excusal policy settings end-to-end.

---

## Phase 10: Polish & Cross-Cutting Concerns

- [x] T116 [P] Verify SMTP error handling: wrap MailKit operations in try/catch in `SmtpEmailNotificationService`; log failures with correlation ID; never throw to caller (email failures must not break primary enrollment flow)
- [x] T117 [P] Add `ExcusalCredit` expiry background check: document that expiry is evaluated at read time (when loading portal or course view, credits past last valid window `EndDate` are returned with status `Expired`); implement expiry resolution in `GetParticipantPortalHandler` and `GetParticipantCourseViewHandler`
- [x] T118 Add rate limiting comment and TODO to `POST /api/v1/participants/magic-link` endpoint in `ParticipantsModule.cs` noting it is a future hardening item
- [x] T119 [P] Verify multi-tenancy: all new DbContext entities have global query filter on `TenantId`; confirm in `RegistrationsDbContext`, `CoursesDbContext`, `TenantsDbContext`
- [x] T120 [P] Verify dependency direction: run `grep -r "using Terminar.Modules" src/Terminar.Modules.*/Domain/` to confirm no domain files import Application or Infrastructure namespaces
- [x] T121 Run full quickstart.md manual validation: request magic link, redeem, view portal, excuse from session, verify credit issued, redeem credit, verify staff credit management, verify settings CRUD

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)         → no dependencies
Phase 2 (Foundational)  → Phase 1 complete — BLOCKS all user stories
Phase 3 (US1)           → Phase 2 complete
Phase 4 (US2)           → Phase 2 complete
Phase 5 (US3)           → Phase 4 complete (adds unenroll to course view)
Phase 6 (US4)           → Phase 4 complete (adds excuse to course view)
Phase 7 (US5)           → Phase 6 complete (credits issued by excusal), Phase 2 (magic link infrastructure)
Phase 8 (US6)           → Phase 7 complete (ExcusalCredit aggregate required)
Phase 9 (US7)           → Phase 2 complete (domain models exist); can run in parallel with US4-6
Phase 10 (Polish)       → all desired phases complete
```

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational (Phase 2). Starts immediately after.
- **US2 (P2)**: Depends on Foundational. Independent of US1.
- **US3 (P3)**: Depends on US2 (unenroll button lives in course view page).
- **US4 (P4)**: Depends on US2 (excuse button lives in course view page).
- **US5 (P4)**: Depends on US4 (credit issued by excusal event). Also depends on ParticipantMagicLink from Foundational.
- **US6 (P4)**: Depends on US5 (ExcusalCredit aggregate must exist).
- **US7 (P5)**: Depends on Foundational domain models. Can run in parallel with US3/US4/US5.

### Parallel Opportunities Within Phases

- Phase 2: T010/T011/T012 and T016/T017 can run in parallel (different modules)
- Phase 3: T029 (email templates) can run in parallel with T030 (SmtpService implementation)
- Phase 6: T048/T049 (enums/events), T050 (aggregate), T051 (repository interface) are sequential within domain; frontend tasks (T062, T064) parallel with backend
- Phase 9: T101/T102/T103/T104/T105/T106/T107 are all parallel (different command/query files)

---

## Parallel Example: Phase 2 (Foundational)

```bash
# These three groups can run in parallel (different modules):
Group A (Registrations): T005 → T006 → T007 → T008 → T009
Group B (Tenants):       T010, T011, T012 → T013 → T014 → T015
Group C (Courses):       T016, T017 → T018 → T019
# After all complete: T020 → T021 → T022 → T023 → T024 → T025 → T026
# T027 (i18n) in parallel with any of the above
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational) — only T005–T009 strictly required for US1
3. Complete Phase 3 (US1)
4. **STOP and VALIDATE**: Confirm real enrollment emails are delivered with safe link
5. Demo to stakeholders

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. **US1** → real enrollment emails (MVP)
3. **US2** → participants can view course via safe link
4. **US3** → participants can unenroll via safe link
5. **US4** → participants can excuse from sessions
6. **US5** → excusal credits + participant portal (magic link login)
7. **US6** → staff can manage credits
8. **US7** → admins can configure policies
9. Polish

---

## Summary

| Phase | User Story | Task Count | Parallelizable |
|---|---|---|---|
| 1 | Setup | 4 | 2 |
| 2 | Foundational | 23 | 8 |
| 3 | US1 — Enrollment Email | 5 | 0 |
| 4 | US2 — Safe Link View | 6 | 4 |
| 5 | US3 — Unenroll | 9 | 2 |
| 6 | US4 — Excuse from Session | 17 | 3 |
| 7 | US5 — Credit Redemption | 26 | 4 |
| 8 | US6 — Staff Management | 9 | 4 |
| 9 | US7 — Tenant Settings | 16 | 12 |
| 10 | Polish | 6 | 4 |
| **Total** | | **121** | |

- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- Each user story produces an independently testable, deliverable increment
- MVP = Phases 1–3 (real enrollment email with safe link)
