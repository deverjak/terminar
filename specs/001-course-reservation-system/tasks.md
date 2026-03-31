---
description: "Task list for Course Reservation System (Termínář) — Feature 001"
---

# Tasks: Course Reservation System (Termínář)

**Input**: Design documents from `/specs/001-course-reservation-system/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api.md ✅

**Tests**: Not included — no TDD requirement in spec. Tests directory stubs created in Setup only.

**Organization**: Phases 1–2 are shared infrastructure. Phases 3–7 map to user stories from spec.md.

## Format: `[ID] [P?] [Story?] Description — file path`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in same phase)
- **[Story]**: User story this task belongs to (US1–US5)

---

## Phase 1: Setup

**Purpose**: Create solution, projects, and wire Aspire orchestration.

- [ ] T001 Create solution file `Terminar.sln` at repo root
- [ ] T002 Create Aspire AppHost project via `dotnet new aspire-apphost -n Terminar.AppHost --output src/Terminar.AppHost`
- [ ] T003 Create Aspire ServiceDefaults project via `dotnet new aspire-servicedefaults -n Terminar.ServiceDefaults --output src/Terminar.ServiceDefaults`
- [ ] T004 [P] Create API project via `dotnet new web -n Terminar.Api --output src/Terminar.Api`
- [ ] T005 [P] Create SharedKernel project via `dotnet new classlib -n Terminar.SharedKernel --output src/Terminar.SharedKernel`
- [ ] T006 [P] Create Tenants module project via `dotnet new classlib -n Terminar.Modules.Tenants --output src/Terminar.Modules.Tenants`
- [ ] T007 [P] Create Identity module project via `dotnet new classlib -n Terminar.Modules.Identity --output src/Terminar.Modules.Identity`
- [ ] T008 [P] Create Courses module project via `dotnet new classlib -n Terminar.Modules.Courses --output src/Terminar.Modules.Courses`
- [ ] T009 [P] Create Registrations module project via `dotnet new classlib -n Terminar.Modules.Registrations --output src/Terminar.Modules.Registrations`
- [ ] T010 [P] Create test projects via `dotnet new xunit` for `tests/Terminar.Modules.Courses.Tests`, `tests/Terminar.Modules.Registrations.Tests`, `tests/Terminar.Api.IntegrationTests`
- [ ] T011 Add all projects to `Terminar.sln` and configure project references: all modules → SharedKernel; Api → all modules; AppHost → Api; test projects → their respective modules
- [ ] T012 Add NuGet packages to each project: `MediatR` (12.x), `FluentValidation` (12.x), `Npgsql.EntityFrameworkCore.PostgreSQL` (10.x), `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` to modules, `StronglyTypedId` (2.x) to SharedKernel and modules, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` to Identity module
- [ ] T013 Configure Aspire AppHost to declare PostgreSQL resource with data volume and reference API project in `src/Terminar.AppHost/Program.cs`

**Checkpoint**: `dotnet build` succeeds with no errors across the solution.

---

## Phase 2: Foundational

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### SharedKernel Base Classes

- [ ] T014 [P] Create `AggregateRoot<TId>` base class with `_domainEvents` collection and `RaiseDomainEvent` / `ClearDomainEvents` methods in `src/Terminar.SharedKernel/AggregateRoot.cs`
- [ ] T015 [P] Create `Entity<TId>` base class with typed ID and value equality in `src/Terminar.SharedKernel/Entity.cs`
- [ ] T016 [P] Create `ValueObject` base class with structural equality via `GetEqualityComponents()` in `src/Terminar.SharedKernel/ValueObject.cs`
- [ ] T017 [P] Create `IDomainEvent` marker interface (implements `MediatR.INotification`) in `src/Terminar.SharedKernel/IDomainEvent.cs`
- [ ] T018 [P] Create `TenantId` strongly-typed Guid wrapper using StronglyTypedId in `src/Terminar.SharedKernel/ValueObjects/TenantId.cs`
- [ ] T019 [P] Create `Email` value object with format validation on construction in `src/Terminar.SharedKernel/ValueObjects/Email.cs`

### API Pipeline & Infrastructure

- [ ] T020 [P] Create `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior that runs all FluentValidation validators and throws `ValidationException` on failure in `src/Terminar.Api/Pipeline/ValidationBehavior.cs`
- [ ] T021 [P] Create `LoggingBehavior<TRequest, TResponse>` MediatR pipeline behavior that logs request name and elapsed time in `src/Terminar.Api/Pipeline/LoggingBehavior.cs`
- [ ] T022 [P] Create `ExceptionHandlingMiddleware` that maps domain exceptions (`ValidationException`, `NotFoundException`, `ConflictException`) to RFC 7807 problem details responses in `src/Terminar.Api/Middleware/ExceptionHandlingMiddleware.cs`
- [ ] T023 [P] Create `ITenantContext` interface with `TenantId` property and scoped `TenantContext` implementation in `src/Terminar.Api/Middleware/TenantContext.cs`
- [ ] T024 Create `TenantResolutionMiddleware` that resolves tenant from JWT `tenant_id` claim (authenticated) or `X-Tenant-Id` header (public), validates it exists, and sets `ITenantContext` in `src/Terminar.Api/Middleware/TenantResolutionMiddleware.cs`
- [ ] T025 Configure `Program.cs`: register MediatR (all module assemblies), FluentValidation, pipeline behaviors, middleware, JWT Bearer auth, problem details, Aspire service defaults, and auto-migration hosted service in `src/Terminar.Api/Program.cs`
- [ ] T026 [P] Create `DatabaseMigrationService` hosted service that applies EF Core migrations for all four module DbContexts on startup in `src/Terminar.Api/Infrastructure/DatabaseMigrationService.cs`

### Tenants Module

- [ ] T027 [P] Create `Tenant` aggregate root with fields: `TenantId`, `Name`, `Slug`, `DefaultLanguageCode`, `Status`, `CreatedAt`; enforce slug format invariant in `src/Terminar.Modules.Tenants/Domain/Tenant.cs`
- [ ] T028 [P] Create `TenantStatus` enum (`Active`, `Suspended`) in `src/Terminar.Modules.Tenants/Domain/TenantStatus.cs`
- [ ] T029 [P] Create `TenantCreated` domain event in `src/Terminar.Modules.Tenants/Domain/Events/TenantCreated.cs`
- [ ] T030 [P] Create `ITenantRepository` interface with `GetByIdAsync`, `GetBySlugAsync`, `AddAsync`, `ExistsAsync` in `src/Terminar.Modules.Tenants/Domain/Repositories/ITenantRepository.cs`
- [ ] T031 Create `CreateTenantCommand` record, `CreateTenantCommandValidator` (validates name, slug uniqueness, language code), and `CreateTenantCommandHandler` (creates tenant + initial Admin StaffUser via cross-module command) in `src/Terminar.Modules.Tenants/Application/Commands/CreateTenant/`
- [ ] T032 [P] Create `GetTenantQuery` record and `GetTenantQueryHandler` in `src/Terminar.Modules.Tenants/Application/Queries/GetTenant/`
- [ ] T033 Create `TenantsDbContext` with `tenants` schema, entity configurations for `Tenant`, and `SaveChangesAsync` override that dispatches domain events via `IMediator` in `src/Terminar.Modules.Tenants/Infrastructure/TenantsDbContext.cs`
- [ ] T034 Create `TenantRepository` implementing `ITenantRepository` using `TenantsDbContext` in `src/Terminar.Modules.Tenants/Infrastructure/Repositories/TenantRepository.cs`
- [ ] T035 Create `TenantsModule` static class with `AddTenantsModule(IServiceCollection)` extension registering DbContext, repository, and MediatR handlers in `src/Terminar.Modules.Tenants/Infrastructure/TenantsModule.cs`
- [ ] T036 Add initial EF Core migration for Tenants module (`tenants` schema, `tenants.tenants` table) via `dotnet ef migrations add InitialCreate --project src/Terminar.Modules.Tenants --startup-project src/Terminar.Api --context TenantsDbContext`
- [ ] T037 Register tenant API routes (`POST /api/v1/tenants`, `GET /api/v1/tenants/{tenantId}`) with `SystemAdmin` authorization policy in `src/Terminar.Api/Modules/TenantsModule.cs`

### Identity Module

- [ ] T038 [P] Create `StaffUser` aggregate root with fields: `StaffUserId`, `TenantId`, `Username`, `Email` (value object), `Role`, `Status`, `CreatedAt`, `LastLoginAt`; enforce deactivated-cannot-login invariant in `src/Terminar.Modules.Identity/Domain/StaffUser.cs`
- [ ] T039 [P] Create `StaffRole` enum (`Admin`, `Staff`, `SystemAdmin`) and `StaffUserStatus` enum (`Active`, `Deactivated`) in `src/Terminar.Modules.Identity/Domain/`
- [ ] T040 [P] Create `StaffUserCreated` domain event in `src/Terminar.Modules.Identity/Domain/Events/StaffUserCreated.cs`
- [ ] T041 [P] Create `IStaffUserRepository` interface with `GetByIdAsync`, `GetByUsernameAsync`, `FindByTenantAsync`, `AddAsync`, `UpdateAsync` in `src/Terminar.Modules.Identity/Domain/Repositories/IStaffUserRepository.cs`
- [ ] T042 Create `AppIdentityUser` class extending `IdentityUser` with additional `TenantId` (Guid) and `Role` (string) fields in `src/Terminar.Modules.Identity/Infrastructure/Identity/AppIdentityUser.cs`
- [ ] T043 Create `AppIdentityDbContext` extending `IdentityDbContext<AppIdentityUser>` using `identity` schema for all ASP.NET Identity tables in `src/Terminar.Modules.Identity/Infrastructure/Identity/AppIdentityDbContext.cs`
- [ ] T044 Create `StaffUserRepository` that wraps `UserManager<AppIdentityUser>` and maps `AppIdentityUser` ↔ `StaffUser` domain aggregate in `src/Terminar.Modules.Identity/Infrastructure/Persistence/StaffUserRepository.cs`
- [ ] T045 Create `JwtTokenService` that issues signed JWT (HS256) with claims `sub`, `tenant_id`, `role`, `exp`, `jti`; manages refresh token storage via `UserManager.SetAuthenticationTokenAsync` in `src/Terminar.Modules.Identity/Infrastructure/Services/JwtTokenService.cs`
- [ ] T046 Create `LoginCommand` record, `LoginCommandValidator`, and `LoginCommandHandler` (verifies password via `SignInManager`, returns JWT + refresh token) in `src/Terminar.Modules.Identity/Application/Auth/Login/`
- [ ] T047 [P] Create `RefreshTokenCommand` record and `RefreshTokenCommandHandler` (validates + rotates refresh token, issues new JWT) in `src/Terminar.Modules.Identity/Application/Auth/RefreshToken/`
- [ ] T048 Create `CreateStaffUserCommand` record, `CreateStaffUserCommandValidator` (validates username/email uniqueness per tenant, password complexity), and `CreateStaffUserCommandHandler` (creates via `UserManager.CreateAsync`) in `src/Terminar.Modules.Identity/Application/Commands/CreateStaffUser/`
- [ ] T049 [P] Create `DeactivateStaffUserCommand` record and `DeactivateStaffUserCommandHandler` (sets status to Deactivated, revokes all refresh tokens) in `src/Terminar.Modules.Identity/Application/Commands/DeactivateStaffUser/`
- [ ] T050 [P] Create `ListStaffUsersQuery` record and `ListStaffUsersQueryHandler` in `src/Terminar.Modules.Identity/Application/Queries/ListStaffUsers/`
- [ ] T051 Create `IdentityModule` static class with `AddIdentityModule(IServiceCollection)` extension: registers `AddIdentityCore<AppIdentityUser>`, `AddEntityFrameworkStores<AppIdentityDbContext>`, `AddJwtBearer` with JWT config from `IConfiguration`, `JwtTokenService`, repository, and MediatR handlers in `src/Terminar.Modules.Identity/Infrastructure/IdentityModule.cs`
- [ ] T052 Add initial EF Core migration for Identity module (`identity` schema, ASP.NET Identity tables) via `dotnet ef migrations add InitialCreate --project src/Terminar.Modules.Identity --startup-project src/Terminar.Api --context AppIdentityDbContext`
- [ ] T053 Register auth and staff API routes: `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/staff`, `GET /api/v1/staff`, `DELETE /api/v1/staff/{staffUserId}` in `src/Terminar.Api/Modules/IdentityModule.cs`

**Checkpoint**: Foundation ready — `dotnet run` from AppHost starts without errors; `POST /api/v1/tenants` and `POST /api/v1/auth/login` return expected responses; tenant resolution middleware resolves tenant from header and JWT claim.

---

## Phase 3: User Story 1 — Staff Creates a Course (Priority: P1) 🎯 MVP

**Goal**: Staff can create, view, update, and cancel courses (one-time and multi-session).

**Independent Test**: Staff logs in, creates a one-time course, and retrieves it by ID with correct session data. No other module needed.

- [ ] T054 [P] [US1] Create `CourseId` and `SessionId` strongly-typed Guid wrappers using StronglyTypedId in `src/Terminar.Modules.Courses/Domain/CourseId.cs` and `src/Terminar.Modules.Courses/Domain/SessionId.cs`
- [ ] T055 [P] [US1] Create `CourseType` enum (`OneTime`, `MultiSession`), `CourseStatus` enum (`Draft`, `Active`, `Cancelled`, `Completed`), and `RegistrationMode` enum (`Open`, `StaffOnly`) in `src/Terminar.Modules.Courses/Domain/`
- [ ] T056 [P] [US1] Create `Session` entity with fields: `SessionId`, `ScheduledAt`, `DurationMinutes`, `Location`, `Sequence`; enforce `ScheduledAt` must not be in the past in `src/Terminar.Modules.Courses/Domain/Session.cs`
- [ ] T057 [US1] Create `Course` aggregate root with fields: `CourseId`, `TenantId`, `Title`, `Description`, `CourseType`, `RegistrationMode`, `Capacity`, `Status`, `Sessions`, `CreatedByStaffId`, `CreatedAt`, `UpdatedAt`; enforce all invariants: OneTime → exactly 1 session, MultiSession → ≥2 sessions, status transition rules, no edit on Cancelled/Completed in `src/Terminar.Modules.Courses/Domain/Course.cs`
- [ ] T058 [P] [US1] Create domain events `CourseCreated`, `CourseActivated`, `CourseCancelled` in `src/Terminar.Modules.Courses/Domain/Events/`
- [ ] T059 [P] [US1] Create `ICourseRepository` interface with `GetByIdAsync`, `ListByTenantAsync`, `AddAsync`, `UpdateAsync` in `src/Terminar.Modules.Courses/Domain/Repositories/ICourseRepository.cs`
- [ ] T060 [P] [US1] Create `ICourseCapacityReader` port interface with `GetCapacityInfoAsync(CourseId, TenantId)` returning capacity and confirmed count in `src/Terminar.Modules.Courses/Application/Ports/ICourseCapacityReader.cs`
- [ ] T061 [US1] Create `CreateCourseCommand` record, `CreateCourseCommandValidator` (validates title, capacity, session count matches course type, no overlapping session times), and `CreateCourseCommandHandler` (creates Course aggregate, persists, raises event) in `src/Terminar.Modules.Courses/Application/Commands/CreateCourse/`
- [ ] T062 [P] [US1] Create `UpdateCourseCommand` record, `UpdateCourseCommandValidator`, and `UpdateCourseCommandHandler` (rejects update if Cancelled/Completed) in `src/Terminar.Modules.Courses/Application/Commands/UpdateCourse/`
- [ ] T063 [P] [US1] Create `CancelCourseCommand` record and `CancelCourseCommandHandler` (transitions status to Cancelled, raises `CourseCancelled` event) in `src/Terminar.Modules.Courses/Application/Commands/CancelCourse/`
- [ ] T064 [P] [US1] Create `ListCoursesQuery` record (with pagination, status filter) and `ListCoursesQueryHandler` (staff sees all statuses; public sees only Active+Open via query parameter flag) in `src/Terminar.Modules.Courses/Application/Queries/ListCourses/`
- [ ] T065 [P] [US1] Create `GetCourseQuery` record and `GetCourseQueryHandler` returning full course with sessions in `src/Terminar.Modules.Courses/Application/Queries/GetCourse/`
- [ ] T066 [US1] Create `CoursesDbContext` with `courses` schema, entity configurations for `Course` and `Session` (owned entity), global `TenantId` query filter, and `SaveChangesAsync` override dispatching domain events in `src/Terminar.Modules.Courses/Infrastructure/CoursesDbContext.cs`
- [ ] T067 [US1] Create `CourseRepository` implementing `ICourseRepository` using `CoursesDbContext` in `src/Terminar.Modules.Courses/Infrastructure/Repositories/CourseRepository.cs`
- [ ] T068 [US1] Create `CourseCapacityReader` implementing `ICourseCapacityReader` (queries CoursesDbContext for capacity; queries RegistrationsDbContext via a cross-module read using a registered `IRegistrationCountReader` port) in `src/Terminar.Modules.Courses/Infrastructure/Ports/CourseCapacityReader.cs`
- [ ] T069 Create `CoursesModule` static class with `AddCoursesModule(IServiceCollection)` registering DbContext (connected to Aspire `terminar-db` resource), repository, capacity reader, and MediatR handlers in `src/Terminar.Modules.Courses/Infrastructure/CoursesModule.cs`
- [ ] T070 Add initial EF Core migration for Courses module (`courses` schema, `courses.courses` and `courses.sessions` tables) via `dotnet ef migrations add InitialCreate --project src/Terminar.Modules.Courses --startup-project src/Terminar.Api --context CoursesDbContext`
- [ ] T071 Register course API routes: `POST /api/v1/courses` (Staff/Admin auth), `GET /api/v1/courses` (public + staff), `GET /api/v1/courses/{courseId}` (public + staff), `PUT /api/v1/courses/{courseId}` (Staff/Admin), `POST /api/v1/courses/{courseId}/cancel` (Admin only) in `src/Terminar.Api/Modules/CoursesModule.cs`

**Checkpoint**: Staff can create both OneTime and MultiSession courses via the API. `GET /api/v1/courses` returns the course list. Creating a OneTime course with 2 sessions returns `422`. Cancelling an already-cancelled course returns `409`.

---

## Phase 4: User Story 2 — Participant Self-Registers (Priority: P2)

**Goal**: Participants can browse open courses and register themselves without a staff account.

**Independent Test**: Public user sends `POST /courses/{id}/registrations` with name + email to an open course → receives `201`. Second identical request returns `409`. Request to a full course returns `422`.

- [ ] T072 [P] [US2] Create `RegistrationId` strongly-typed Guid wrapper using StronglyTypedId in `src/Terminar.Modules.Registrations/Domain/RegistrationId.cs`
- [ ] T073 [P] [US2] Create `RegistrationStatus` enum (`Confirmed`, `Cancelled`) and `RegistrationSource` enum (`SelfService`, `StaffAdded`) in `src/Terminar.Modules.Registrations/Domain/`
- [ ] T074 [P] [US2] Create `RegistrationCreated` and `RegistrationCancelled` domain events in `src/Terminar.Modules.Registrations/Domain/Events/`
- [ ] T075 [P] [US2] Create `IRegistrationRepository` interface with `GetByIdAsync`, `GetByEmailAndCourseAsync`, `CountConfirmedByCourseAsync`, `AddAsync`, `UpdateAsync` in `src/Terminar.Modules.Registrations/Domain/Repositories/IRegistrationRepository.cs`
- [ ] T076 [P] [US2] Create `IRegistrationCountReader` port interface (used by Courses module's `CourseCapacityReader`) with `CountConfirmedAsync(CourseId, TenantId)` in `src/Terminar.Modules.Registrations/Application/Ports/IRegistrationCountReader.cs`
- [ ] T077 [US2] Create `RegistrationCapacityChecker` domain service that uses `ICourseCapacityReader` to check if registration is allowed (throws `CourseFullException` if confirmed count >= capacity) in `src/Terminar.Modules.Registrations/Domain/Services/RegistrationCapacityChecker.cs`
- [ ] T078 [US2] Create `Registration` aggregate root with all fields from data-model.md; enforce invariants: no duplicate confirmed registration for same email+course, no registration on Cancelled/Completed course, no cancel after all sessions ended; generate `CancellationToken` (random Guid) on creation in `src/Terminar.Modules.Registrations/Domain/Registration.cs`
- [ ] T079 [US2] Create `CreateRegistrationCommand` record (courseId, participantName, participantEmail, optional registeredByStaffId), `CreateRegistrationCommandValidator`, and `CreateRegistrationCommandHandler` (checks capacity via `RegistrationCapacityChecker`, creates aggregate, persists, raises `RegistrationCreated`) in `src/Terminar.Modules.Registrations/Application/Commands/CreateRegistration/`
- [ ] T080 [US2] Create `RegistrationsDbContext` with `registrations` schema, entity configuration for `Registration`, global `TenantId` query filter, and `SaveChangesAsync` override dispatching domain events in `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsDbContext.cs`
- [ ] T081 [US2] Create `RegistrationRepository` implementing `IRegistrationRepository` and `IRegistrationCountReader` using `RegistrationsDbContext` in `src/Terminar.Modules.Registrations/Infrastructure/Repositories/RegistrationRepository.cs`
- [ ] T082 Create `RegistrationsModule` static class with `AddRegistrationsModule(IServiceCollection)` registering DbContext, repository (as both `IRegistrationRepository` and `IRegistrationCountReader`), domain service, and MediatR handlers in `src/Terminar.Modules.Registrations/Infrastructure/RegistrationsModule.cs`
- [ ] T083 Add initial EF Core migration for Registrations module (`registrations` schema, `registrations.registrations` table) via `dotnet ef migrations add InitialCreate --project src/Terminar.Modules.Registrations --startup-project src/Terminar.Api --context RegistrationsDbContext`
- [ ] T084 Register public self-registration route `POST /api/v1/courses/{courseId}/registrations` (no auth required, reads tenant from `X-Tenant-Id` header, enforces `Open` registration mode) in `src/Terminar.Api/Modules/RegistrationsModule.cs`

**Checkpoint**: Public user registers for an open course. Duplicate registration returns `409`. Registration on a StaffOnly course returns `403`. Course at capacity returns `422`.

---

## Phase 5: User Story 3 — Staff Registers Participant Manually (Priority: P2)

**Goal**: Staff can add participants to any course regardless of registration mode.

**Independent Test**: Authenticated staff sends `POST /courses/{id}/registrations` to a `StaffOnly` course → participant appears on roster. Same as US2 endpoint but with Bearer token and no registration mode restriction.

- [ ] T085 [US3] Extend `CreateRegistrationCommandHandler` to detect caller role from command context: if `RegisteredByStaffId` is provided, set `RegistrationSource.StaffAdded` and bypass `Open` mode restriction; if registering past capacity, return a `409` warning response (no silent override) in `src/Terminar.Modules.Registrations/Application/Commands/CreateRegistration/CreateRegistrationCommandHandler.cs`
- [ ] T086 [US3] Add authenticated staff registration route to the same `POST /api/v1/courses/{courseId}/registrations` endpoint: require `Staff` or `Admin` JWT, pass `RegisteredByStaffId` from JWT `sub` claim to the command in `src/Terminar.Api/Modules/RegistrationsModule.cs`

**Checkpoint**: Staff registers a participant on a `StaffOnly` course → `201`. Public user attempting the same → `403`. Staff registering duplicate email → `409`.

---

## Phase 6: User Story 5 — Staff Views Course Roster (Priority: P2)

**Goal**: Staff can retrieve the full paginated list of participants registered for any course.

**Independent Test**: Staff sends `GET /courses/{id}/registrations` with JWT → receives paginated roster with all confirmed registrations. Unauthenticated request → `401`.

- [ ] T087 [P] [US5] Create `GetCourseRosterQuery` record (courseId, tenantId, status filter, page, pageSize) and `GetCourseRosterQueryHandler` returning paginated `RegistrationDto` list in `src/Terminar.Modules.Registrations/Application/Queries/GetCourseRoster/`
- [ ] T088 [US5] Register roster route `GET /api/v1/courses/{courseId}/registrations` (requires `Staff` or `Admin` JWT, supports `?status=Confirmed|Cancelled&page=1&page_size=20`) in `src/Terminar.Api/Modules/RegistrationsModule.cs`

**Checkpoint**: Staff retrieves roster with correct pagination. Filtering by `?status=Cancelled` shows only cancelled registrations. Total count in response matches database.

---

## Phase 7: User Story 4 — Participant Cancels Registration (Priority: P3)

**Goal**: Registered participants can cancel their own registration using a cancellation token; staff can cancel any registration with their JWT.

**Independent Test**: Participant uses cancellation token from registration response to call `DELETE /courses/{id}/registrations/{regId}?token=<token>` → `204`. Second call with same token → `409`.

- [ ] T089 [P] [US4] Extend `Registration` aggregate to include `CancellationToken` (Guid, generated on creation) and `Cancel(DateTimeOffset now, IEnumerable<Session> sessions)` method that validates cancellation window (all sessions must not have ended) and sets status to Cancelled in `src/Terminar.Modules.Registrations/Domain/Registration.cs`
- [ ] T090 [P] [US4] Add `CancellationToken` to `RegistrationsDbContext` entity configuration and create EF Core migration `AddCancellationToken` in `src/Terminar.Modules.Registrations/Infrastructure/`
- [ ] T091 [US4] Create `CancelRegistrationCommand` record (registrationId, courseId, tenantId, cancellationToken optional, staffUserId optional), `CancelRegistrationCommandValidator`, and `CancelRegistrationCommandHandler` (validates: token matches OR caller is staff; calls `Registration.Cancel()`; raises `RegistrationCancelled`) in `src/Terminar.Modules.Registrations/Application/Commands/CancelRegistration/`
- [ ] T092 [US4] Extend `CreateRegistrationCommand` response DTO to include `cancellation_token` field (returned once at registration, never again) in `src/Terminar.Modules.Registrations/Application/Commands/CreateRegistration/`
- [ ] T093 [US4] Register cancellation route `DELETE /api/v1/courses/{courseId}/registrations/{registrationId}` (public with `?token=<token>` OR Staff/Admin JWT) in `src/Terminar.Api/Modules/RegistrationsModule.cs`

**Checkpoint**: Self-cancellation with valid token → `204`. Cancellation with wrong token → `403`. Staff cancellation with JWT → `204`. Cancellation of already-cancelled registration → `409`. Cancellation after all sessions ended → `422`.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finalize shared concerns affecting all user stories.

- [ ] T094 [P] Create `IEmailNotificationService` interface with `SendRegistrationConfirmationAsync` and `SendRegistrationCancellationAsync` in `src/Terminar.Api/Notifications/IEmailNotificationService.cs`
- [ ] T095 [P] Create `StubEmailNotificationService` (no-op implementation, logs to console) and `RegistrationCreatedEmailHandler` (`INotificationHandler<RegistrationCreated>`) in `src/Terminar.Api/Notifications/`
- [ ] T096 [P] Add health check endpoints for each module DbContext via `AddNpgsqlHealthCheck` in `src/Terminar.Api/Program.cs`
- [ ] T097 Validate `quickstart.md` end-to-end walkthrough against running system: create tenant → login → create course → self-register → view roster → cancel registration

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 — no dependency on other stories
- **US2 (Phase 4)**: Depends on Phase 2 — no dependency on US1 (but needs ICourseCapacityReader which Phase 3 provides; implement a stub or complete Phase 3 first)
- **US3 (Phase 5)**: Depends on Phase 4 — extends the Registration aggregate and handler
- **US5 (Phase 6)**: Depends on Phase 4 — adds a query on top of existing infrastructure
- **US4 (Phase 7)**: Depends on Phase 4 — extends the Registration aggregate with cancellation
- **Polish (Phase 8)**: Depends on all user story phases

### Note on US2 / Phase 3 Dependency

`RegistrationCapacityChecker` calls `ICourseCapacityReader` (defined in Courses module, Phase 3). Two options:
1. Complete Phase 3 before Phase 4 (recommended — natural priority order)
2. Register a stub `ICourseCapacityReader` in Phase 4, replace in Phase 3 (if teams run in parallel)

### Within Each Phase — Parallel Opportunities

- SharedKernel tasks T014–T019: all [P], run simultaneously
- Pipeline + middleware T020–T022: all [P]
- Tenants domain tasks T027–T030: all [P]
- Identity domain tasks T038–T041: all [P] (independent of Tenants domain)
- Identity infrastructure T042–T045: sequential (T043 depends on T042)
- US1 domain tasks T054–T060: T054–T058 are [P]; T057 depends on T056
- US2 domain tasks T072–T076: all [P]

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational — SharedKernel + Tenants + Identity + Middleware
3. Complete Phase 3: User Story 1 — Courses module
4. **STOP and VALIDATE**: Create tenant → login → create course → retrieve course
5. Deploy / demo MVP

### Incremental Delivery

| Step | Phases | Deliverable |
|------|--------|-------------|
| 1 | 1 + 2 | Auth, tenant management, API skeleton |
| 2 | + 3 | Course creation and browsing (MVP) |
| 3 | + 4 | Self-registration |
| 4 | + 5 | Staff manual registration |
| 5 | + 6 | Roster view |
| 6 | + 7 | Self-cancellation |
| 7 | + 8 | Email notifications, health checks |

### Parallel Team Strategy

With two developers after Phase 2 (Foundational) is complete:
- **Dev A**: Phase 3 (Courses module)
- **Dev B**: Phases 5–6 can start with a stub ICourseCapacityReader; merge once Phase 3 delivers the real implementation

---

## Notes

- `[P]` = different files, no blocking dependencies within same phase
- `[Story]` label maps each task to its user story for independent delivery tracking
- Commit after each phase checkpoint
- EF Core migrations: one migration per module; run add-migration after completing Infrastructure layer of each module
- `StronglyTypedId`: add `[StronglyTypedId]` attribute to generate Guid wrapper; register EF Core value converters in each DbContext
- Cross-module port pattern: `ICourseCapacityReader` defined in Courses module, implemented there; `IRegistrationCountReader` defined in Registrations module, also implemented there — both registered in DI in their respective `Module.cs` files
