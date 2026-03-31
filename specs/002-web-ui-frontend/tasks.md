# Tasks: Web UI Frontend

**Input**: Design documents from `/specs/002-web-ui-frontend/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-client.md ✅, quickstart.md ✅

**Tests**: Not requested — no test tasks generated.

**Organization**: Tasks grouped by user story (US1–US7) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US7)
- All paths are relative to repository root

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Bootstrap the Vite + React project and install all dependencies.

- [x] T001 Initialize Vite + React + TypeScript project at `frontend/` using `pnpm create vite frontend --template react-ts`
- [x] T002 Install all dependencies per quickstart.md, mantine in v9: `@mantine/core`, `@mantine/hooks`, `@mantine/form`, `@mantine/dates`, `@mantine/notifications`, `react-router`, `@tanstack/react-query`, `i18next`, `react-i18next`, `dayjs` in `frontend/`
- [x] T003 [P] Configure `frontend/tsconfig.json` with strict mode, path aliases (`@/` → `src/`), and `moduleResolution: bundler`
- [x] T004 [P] Configure `frontend/vite.config.ts` with path alias `@` → `src/`, `VITE_API_BASE_URL` env var support
- [x] T005 [P] Create `frontend/.env.local` template with `VITE_API_BASE_URL=http://localhost:5000` and add `frontend/.env.local` to `.gitignore`
- [x] T006 Create the full folder skeleton: `frontend/src/app/`, `frontend/src/features/landing/`, `frontend/src/features/auth/`, `frontend/src/features/tenants/`, `frontend/src/features/courses/`, `frontend/src/features/registrations/`, `frontend/src/features/staff/`, `frontend/src/shared/api/`, `frontend/src/shared/components/`, `frontend/src/shared/hooks/`, `frontend/src/shared/i18n/locales/`

**Checkpoint**: `pnpm dev` in `frontend/` starts the Vite dev server without errors.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared infrastructure every user story depends on — API client, auth, i18n, theme, app shell, and routing skeleton. No user story work begins until this phase is complete.

**⚠️ CRITICAL**: No user story phase can start until Phase 2 is complete.

- [x] T007 Create `frontend/src/shared/api/client.ts` — typed `apiFetch` wrapper that reads `VITE_API_BASE_URL`, injects `Authorization: Bearer {accessToken}` and `X-Tenant-Slug: {tenantSlug}` headers from auth context, handles 401 by attempting one silent token refresh, then throws on failure
- [x] T008 Create `frontend/src/features/auth/AuthContext.tsx` — React context holding `{ accessToken, refreshToken, userId, username, role, tenantSlug, tenantId }` state; exposes `login()`, `logout()`, `refreshSession()` methods; on mount, reads `refreshToken` + `tenantSlug` from `localStorage` and silently calls `POST /api/v1/auth/refresh` to restore session before rendering children
- [x] T009 Create `frontend/src/features/auth/useAuth.ts` — convenience hook that returns the `AuthContext` value; throws if used outside `AuthProvider`
- [x] T010 [P] Create `frontend/src/shared/i18n/locales/en.json` — English translation file with keys for every user-facing string across all 7 user stories: nav items, form labels, button labels, status values (CourseStatus, RegistrationStatus, StaffUser status), error messages, empty states, page titles, confirmation dialogs
- [x] T011 [P] Create `frontend/src/shared/i18n/locales/cs.json` — Czech translation file with the same key structure as `en.json`, all values translated to Czech
- [x] T012 Create `frontend/src/shared/i18n/index.ts` — configure `i18next` with `react-i18next`, auto-detect browser language (`i18next-browser-languagedetector`), fallback to `en`, load `en.json` and `cs.json` resources
- [x] T013 Create `frontend/src/app/theme.ts` — Mantine custom theme object; configure primary color; export `colorSchemeManager` using `localStorageColorSchemeManager` so user preference persists across sessions
- [x] T014 Create `frontend/src/app/providers.tsx` — wraps children with `MantineProvider` (theme + colorScheme), `Notifications`, `QueryClientProvider` (TanStack Query), `AuthProvider` (from T008), and i18next `I18nextProvider`
- [x] T015 Create `frontend/src/shared/components/AppShellLayout.tsx` — Mantine `AppShell` with: left nav sidebar (links: Courses, Staff), top header with app name, light/dark toggle button (`useColorScheme` hook), language switcher (EN/CS), and logout button; Staff nav link visible only when `role === 'Admin'`
- [x] T016 Create `frontend/src/shared/components/ConfirmModal.tsx` — reusable Mantine `Modal` that accepts `title`, `message`, `onConfirm`, `onCancel`, `loading` props; used for cancel course, deactivate staff, cancel registration confirmations
- [x] T017 [P] Create `frontend/src/shared/components/StatusBadge.tsx` — Mantine `Badge` component that maps `CourseStatus`, `RegistrationStatus`, and staff `Status` enum values to appropriate colors and translated labels
- [x] T018 [P] Create `frontend/src/shared/hooks/usePagination.ts` — hook that manages `{ page, pageSize }` state with `setPage`, `setPageSize` helpers; default `pageSize = 20`
- [x] T019 Create `frontend/src/app/router.tsx` — React Router v7 route tree:
  - `/` → `LandingPage`
  - `/register` → `TenantRegisterPage`
  - `/login` → `LoginPage`
  - `/app` → `AppShellLayout` (requires auth; redirects to `/login` if no session)
    - `/app/courses` → `CourseListPage`
    - `/app/courses/new` → `CreateCoursePage`
    - `/app/courses/:id` → `CourseDetailPage`
    - `/app/courses/:id/edit` → `EditCoursePage`
    - `/app/courses/:courseId/registrations` → `CourseRosterPage`
    - `/app/staff` → `StaffListPage` (requires Admin role; 403 redirect otherwise)
    - `/app/staff/new` → redirects to `StaffListPage` with create modal open
  - `*` → `NotFoundPage`
- [x] T020 Create `frontend/src/main.tsx` — entry point that renders `<Providers><RouterProvider router={router} /></Providers>` with i18n initialized before render
- [x] T021 Create `frontend/src/shared/components/NotFoundPage.tsx` — simple 404 page with link back to `/`

**Checkpoint**: App starts, shows a blank AppShell with nav, color toggle works, language toggle works, `/login` route loads without crash.

---

## Phase 3: User Story 1 — Tenant Onboarding via Landing Page (Priority: P1) 🎯 MVP

**Goal**: A new user visits the app, creates a tenant with admin credentials, and lands on the authenticated dashboard.

**Independent Test**: Open the app cold, click "Create your workspace", fill in name, slug, language, username, email, password — submit and verify redirect to `/app/courses` with an authenticated session.

- [x] T022 [US1] Create `frontend/src/features/tenants/tenantsApi.ts` — typed `createTenant(data: CreateTenantRequest): Promise<CreateTenantResponse>` function calling `POST /api/v1/tenants` (no auth headers needed)
- [x] T023 [US1] Create `frontend/src/features/landing/LandingPage.tsx` — full-screen hero section with app name, tagline, and two prominent buttons: "Create your workspace" (→ `/register`) and "Log in" (→ `/login`); no auth required; already-authenticated users are redirected to `/app/courses`
- [x] T024 [US1] Create `frontend/src/features/tenants/TenantRegisterPage.tsx` — Mantine form with fields: Organization name (required), Slug (required, lowercase/hyphens only, 3–63 chars, with live format hint), Default language (select: English / Czech), Admin username (required), Admin email (required, email format), Admin password (required, min 8 chars), Confirm password (must match); on submit calls `createTenant()` from `tenantsApi.ts`, then immediately calls `login()` from `authApi.ts` with the admin credentials and tenant slug, then redirects to `/app/courses`; slug-conflict 422 error surfaced as inline field error; all other errors shown as Mantine notifications

**Checkpoint (US1)**: Tenant creation form works end-to-end — submitting valid data creates a tenant, logs in the admin, and navigates to the course list.

---

## Phase 4: User Story 2 — Staff Login to Existing Tenant (Priority: P1)

**Goal**: A returning staff user enters their slug + credentials, authenticates, and lands on the course list.

**Independent Test**: Open `/login`, enter a valid tenant slug, username, and password — verify redirect to `/app/courses` and that the auth context has the correct user info.

- [x] T025 [US2] Create `frontend/src/features/auth/authApi.ts` — typed functions: `login(slug, username, password): Promise<AuthTokenResponse>` calling `POST /api/v1/auth/login` with `X-Tenant-Slug` header (no auth token); `refreshToken(userId, refreshToken): Promise<AuthTokenResponse>` calling `POST /api/v1/auth/refresh`
- [x] T026 [US2] Create `frontend/src/features/auth/LoginPage.tsx` — Mantine form with fields: Tenant slug (required), Username (required), Password (required); on submit calls `authApi.login()`, stores tokens + slug in `AuthContext`, redirects to `/app/courses`; maps error responses: 401 → "Invalid credentials" inline, 403 → "Account is inactive" notification; shows loading spinner on submit; already-authenticated users redirect to `/app/courses`

**Checkpoint (US2)**: Login flow works — valid credentials authenticate and navigate to course list; invalid credentials show inline error; deactivated account shows notification.

---

## Phase 5: User Story 3 — Course List and Calendar View (Priority: P2)

**Goal**: An authenticated staff member views all tenant courses in a list view and toggles to a calendar view showing sessions by date.

**Independent Test**: Log in, navigate to `/app/courses` — verify the course list loads, shows name/status/session count, and toggling to calendar view shows sessions as date badges.

- [x] T027 [US3] Create `frontend/src/features/courses/types.ts` — TypeScript interfaces matching `data-model.md`: `CourseListItem`, `CourseDetail`, `SessionDetail`, `CreateCourseRequest`, `SessionInput`, `UpdateCourseRequest`, enums `CourseType`, `RegistrationMode`, `CourseStatus`, and `CalendarEvent`
- [x] T028 [US3] Create `frontend/src/features/courses/coursesApi.ts` — typed functions: `listCourses(): Promise<CourseListItem[]>`, `getCourse(id): Promise<CourseDetail>`, `createCourse(data): Promise<{ id: string }>`, `updateCourse(id, data): Promise<void>`, `cancelCourse(id): Promise<void>` — all using the `apiFetch` wrapper from `shared/api/client.ts`
- [x] T029 [US3] Create `frontend/src/features/courses/CourseListPage.tsx` — uses TanStack Query `useQuery(['courses'], listCourses)` to fetch courses; renders Mantine `Table` with columns: Title, Type, Status (`StatusBadge`), Capacity, Sessions, First Session; each row has "View" link to `/app/courses/:id`; "New Course" button navigates to `/app/courses/new`; "Calendar" toggle button switches to `CourseCalendarPage`; empty state with prompt when no courses; loading skeleton while fetching
- [x] T030 [US3] Create `frontend/src/features/courses/CourseCalendarPage.tsx` — derives `CalendarEvent[]` from the `listCourses` query (sessions from `firstSessionAt` on each `CourseListItem`); renders a monthly calendar grid using `@mantine/dates`; sessions appear as colored `Badge` elements on their date cell; clicking a badge navigates to `/app/courses/:id`; month navigation (prev/next); "List" toggle returns to `CourseListPage`

**Checkpoint (US3)**: Course list and calendar both load with real data; empty state renders when tenant has no courses; toggling between views works.

---

## Phase 6: User Story 4 — Create New Course (Priority: P2)

**Goal**: An authenticated staff member creates a new course with at least one session, and the course appears in the list.

**Independent Test**: Navigate to `/app/courses/new`, fill in title, description, type, mode, capacity, and add one future-dated session — submit and verify the course appears in the list and calendar.

- [x] T031 [US4] Create `frontend/src/features/courses/CreateCoursePage.tsx` — Mantine form with: Title (required), Description (textarea, optional), Course Type (Select: OneTime / MultiSession), Registration Mode (Select: Open / StaffOnly), Capacity (number input, min 1); dynamic session list section with "Add Session" button — each session row has scheduledAt (DateTimePicker, must be future, validated on blur), Duration in minutes (number, min 1), Location (text, optional), and a remove button; on submit calls `createCourse()`, invalidates `['courses']` TanStack Query cache, navigates to `/app/courses/:newId`; past session date shows inline field error; all API errors shown as notifications
- [x] T032 [US4] Create `frontend/src/features/courses/EditCoursePage.tsx` — pre-loads course via `getCourse(id)` (`useQuery`); Mantine form pre-populated with title, description, capacity, registrationMode (editable fields only — type and sessions are immutable after creation); on submit calls `updateCourse(id, data)`, invalidates `['course', id]` and `['courses']` caches, navigates back to `/app/courses/:id`; uses the same validation as create form

**Checkpoint (US4)**: Full create flow works — form validates future dates, submits successfully, new course appears in list and calendar; edit form pre-populates and saves changes.

---

## Phase 7: User Story 5 — Course Detail and Session View (Priority: P3)

**Goal**: An authenticated staff member views full course details including all scheduled sessions with dates, times, and locations.

**Independent Test**: Navigate to `/app/courses/:id` — verify course metadata, session list with sequence/date/time/duration/location/endsAt all display correctly; Edit and Cancel Course actions are accessible.

- [x] T033 [US5] Create `frontend/src/features/courses/CourseDetailPage.tsx` — uses `useQuery(['course', id], () => getCourse(id))`; renders: course header (title, status badge, type, mode, capacity); description; sessions list as Mantine `Table` (Sequence, Date, Time, Duration, Location, Ends At — times formatted in local timezone using `dayjs`); action buttons: "Edit Course" (→ `/app/courses/:id/edit`, visible when status is not Cancelled/Completed), "Cancel Course" (opens `ConfirmModal`, calls `cancelCourse(id)` on confirm, invalidates caches, Admin-only); "View Roster" button (→ `/app/courses/:id/registrations`); loading skeleton while fetching; 404 handling if course not found

**Checkpoint (US5)**: Course detail page renders complete course data including all sessions; Cancel Course modal confirms before action; times display in local timezone.

---

## Phase 8: User Story 6 — Registration Management (Priority: P3)

**Goal**: An authenticated staff member views a course's registration roster, adds new registrations, and can cancel existing ones.

**Independent Test**: Navigate to `/app/courses/:courseId/registrations` — verify paginated roster loads; click "Add Registration", fill name and email, submit — verify it appears in roster with status Confirmed; cancel a registration and verify status changes.

- [x] T034 [US6] Create `frontend/src/features/registrations/registrationsApi.ts` — typed functions: `getRoster(courseId, page, pageSize, statusFilter?): Promise<RosterPage>`, `createRegistration(courseId, data): Promise<RegistrationCreated>`, `cancelRegistration(courseId, registrationId): Promise<void>`
- [x] T035 [US6] Create `frontend/src/features/registrations/CreateRegistrationModal.tsx` — Mantine `Modal` with Participant Name (required) and Participant Email (required, email format) inputs; on submit calls `createRegistration()`, invalidates roster query, closes modal; 409 (full) error shown as inline message "This course is at full capacity"; other errors as notifications
- [x] T036 [US6] Create `frontend/src/features/registrations/CourseRosterPage.tsx` — uses `useQuery` with `['roster', courseId, page, pageSize]` key; renders: breadcrumb back to course detail; roster `Table` with columns: Participant Name, Email, Source (`StatusBadge`), Status (`StatusBadge`), Registered At; "Cancel" action per row (opens `ConfirmModal`, calls `cancelRegistration()`, re-fetches); "Add Registration" button (opens `CreateRegistrationModal`); Mantine `Pagination` component for paging; status filter (All / Confirmed / Cancelled) using `Select`; empty state when no registrations

**Checkpoint (US6)**: Roster loads with pagination; add registration modal validates and submits; full-capacity error shows correctly; cancel registration updates the list.

---

## Phase 9: User Story 7 — Staff User Management (Priority: P4)

**Goal**: An authenticated Admin user views all staff accounts in the tenant, creates new staff users, and deactivates existing ones.

**Independent Test**: Log in as Admin, navigate to `/app/staff` — verify staff list loads with all users; click "Add Staff", fill in details, submit — verify new user appears; click "Deactivate" on a user, confirm — verify status changes to Inactive.

- [x] T037 [US7] Create `frontend/src/features/staff/staffApi.ts` — typed functions: `listStaff(): Promise<StaffUser[]>`, `createStaff(data: CreateStaffUserRequest): Promise<StaffUser>`, `deactivateStaff(id: string): Promise<void>`
- [x] T038 [US7] Create `frontend/src/features/staff/CreateStaffModal.tsx` — Mantine `Modal` with: Username (required), Email (required, email format), Password (required, min 8 chars), Role (Select: Staff / Admin); on submit calls `createStaff()`, invalidates staff query, closes modal; API errors (e.g. duplicate username) shown as inline or notification
- [x] T039 [US7] Create `frontend/src/features/staff/StaffListPage.tsx` — uses `useQuery(['staff'], listStaff)`; renders Mantine `Table` with: Username, Email, Role (`StatusBadge`), Status (`StatusBadge`), Created At; "Deactivate" button per active row (opens `ConfirmModal`, calls `deactivateStaff()`, invalidates query); "Add Staff" button opens `CreateStaffModal`; Admin-only: non-Admin users who navigate here directly see a 403 message and a back link; empty state for no staff

**Checkpoint (US7)**: Staff list loads for Admin; deactivate confirms and updates status; create modal validates and adds new staff; non-Admin users see access denied.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Error handling, loading states, empty states, global UX polish across all user stories.

- [x] T040 [P] Add global error boundary in `frontend/src/app/providers.tsx` wrapping the router — catches unexpected React errors and shows a friendly "Something went wrong" Mantine `Alert` with a reload button
- [x] T041 [P] Add loading skeletons to all data-fetching pages: `CourseListPage`, `CourseDetailPage`, `CourseRosterPage`, `StaffListPage` — use Mantine `Skeleton` components matching the shape of the loaded content
- [x] T042 [P] Implement automatic 401 retry in `frontend/src/shared/api/client.ts` — on first 401, call `refreshSession()` from `AuthContext`, retry the original request once; if retry fails or refresh returns 401, call `logout()` and redirect to `/login`
- [x] T043 [P] Add Mantine `Notifications` toast for all success mutations — "Course created", "Registration added", "Staff user deactivated", etc. — using `showNotification` after each successful mutation in the relevant page/modal components
- [x] T044 [P] Map 422 validation error response bodies to Mantine form field errors in all form components (`TenantRegisterPage`, `LoginPage`, `CreateCoursePage`, `EditCoursePage`, `CreateRegistrationModal`, `CreateStaffModal`) — parse `{ errors: { field: [messages] } }` response shape and set field errors via `form.setFieldError()`
- [x] T045 Update `frontend/src/app/router.tsx` to set `document.title` per route using React Router's `<title>` in route meta or a `useEffect` in each page component — format: `"{Page Name} — Termínář"`
- [x] T046 [P] Verify `en.json` and `cs.json` in `frontend/src/shared/i18n/locales/` have no missing keys (all keys in `en.json` exist in `cs.json`) and no hardcoded user-facing strings remain in any component — fix any found
- [x] T047 [P] Update `frontend/README.md` with: dev setup steps, env var documentation, available scripts (`dev`, `build`, `preview`), and link to `specs/002-web-ui-frontend/quickstart.md`

**Checkpoint**: All pages have loading states, empty states, and error notifications; 401 auto-refresh works; page titles update; no hardcoded UI strings remain.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user story phases
- **Phase 3 (US1) and Phase 4 (US2)**: Both P1; depend on Phase 2; can run in parallel
- **Phase 5 (US3) and Phase 6 (US4)**: Both P2; depend on Phase 2; can run in parallel (US4 creates data that helps test US3)
- **Phase 7 (US5)**: Depends on Phase 5 (US3) — course detail extends the list view
- **Phase 8 (US6)**: Depends on Phase 5 (US3) — roster linked from course detail; can parallel with Phase 7
- **Phase 9 (US7)**: Depends on Phase 2 only — staff management is independent of course features
- **Phase 10 (Polish)**: Depends on all desired phases being complete

### User Story Dependencies

| Story                      | Can Start After | Parallel With |
| -------------------------- | --------------- | ------------- |
| US1 (Tenant Onboarding)    | Phase 2         | US2           |
| US2 (Login)                | Phase 2         | US1           |
| US3 (Course List/Calendar) | Phase 2         | US4, US7      |
| US4 (Create Course)        | Phase 2         | US3, US7      |
| US5 (Course Detail)        | US3 complete    | US6           |
| US6 (Registrations)        | US3 complete    | US5           |
| US7 (Staff Management)     | Phase 2         | US3, US4      |

### Within Each Phase

- Models / types → API functions → UI components (in that order)
- [P]-marked tasks within a phase have no intra-phase dependencies

### Parallel Opportunities

All [P]-marked tasks within a phase can be launched simultaneously. Key parallel batches:

```
# Phase 1 parallel batch:
T003, T004, T005

# Phase 2 parallel batch (after T007, T008, T009 complete):
T010, T011  ← i18n locale files
T017, T018  ← shared components

# Phase 10 parallel batch:
T040, T041, T042, T043, T044, T046, T047
```

---

## Implementation Strategy

### MVP First (P1 Stories Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks everything)
3. Complete Phase 3: US1 (Tenant Onboarding)
4. Complete Phase 4: US2 (Login)
5. **STOP and VALIDATE**: Full auth lifecycle works end-to-end
6. Demo: new user registers tenant, logs in, sees empty course list

### Incremental Delivery

1. Setup + Foundational → app shell running
2. US1 + US2 → auth complete → **Demo 1**
3. US3 + US4 → courses readable and writable → **Demo 2**
4. US5 → course detail with sessions → **Demo 3**
5. US6 → registrations roster → **Demo 4**
6. US7 → staff management → **Demo 5**
7. Polish → production-ready → **Final Release**

### Parallel Team Strategy

With 2–3 developers, after Phase 2 completes:

- **Dev A**: US1 + US2 (auth flows, landing, forms)
- **Dev B**: US3 + US4 (course list, calendar, create/edit)
- **Dev C**: US7 (staff management — fully independent)

Then merge and complete US5, US6, Polish together.

---

## Notes

- [P] = task touches different files from other [P] tasks in the same phase; no intra-phase dependency
- Each user story phase ends at a **Checkpoint** — validate independently before proceeding
- All i18n keys must be added to `en.json` AND `cs.json` in the same task (T010/T011 are paired)
- The `apiFetch` wrapper (T007) is the single source of auth headers — never add headers manually in feature API files
- `AuthContext` (T008) owns token storage strategy — no other file reads/writes `localStorage` auth keys
- Commit after each checkpoint at minimum
