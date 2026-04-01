# Implementation Plan: Web UI Frontend

**Branch**: `002-web-ui-frontend` | **Date**: 2026-03-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-web-ui-frontend/spec.md`

## Summary

Build a React 19 + Mantine v9 single-page application that provides a complete web GUI for the Termínář backend API. The frontend includes a public landing page with tenant registration and login flows, and an authenticated area for managing courses (list + calendar), course sessions, registrations, and staff users. The app uses React Router v7, TanStack Query v5 for server state, react-i18next for mandatory internationalisation, and Mantine's built-in theming for light/dark mode.

## Technical Context

**Language/Version**: TypeScript 5.x, React 19, Node.js 22+
**Primary Dependencies**: Mantine v9, React Router v7, TanStack Query v5, react-i18next, Vite 6
**Storage**: No database — all state via backend API; access token in memory, refresh token + tenant slug in localStorage
**Testing**: Vitest + React Testing Library (unit/integration); Playwright (E2E)
**Target Platform**: Desktop web browser (modern Chromium, Firefox, Safari)
**Project Type**: Single-page web application (SPA)
**Performance Goals**: First meaningful paint < 2s on localhost; list views < 500ms perceived load with TanStack Query cache
**Constraints**: No server-side rendering; offline mode not required; desktop-only (no mobile breakpoints required in v1)
**Scale/Scope**: ~10 pages/views, ~20 reusable components, 4 API modules consumed

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Domain-Driven Design** | ✅ Pass | Applies to the backend only. The frontend is the Presentation layer — DDD boundaries are enforced at the API surface, not within the SPA itself. |
| **II. Multi-Tenancy by Default** | ✅ Pass | Every authenticated API call sends `X-Tenant-Slug` header. Auth context stores and enforces tenant scope. No cross-tenant data access is possible from the frontend. |
| **III. Multi-Language First** | ✅ Pass | `react-i18next` is included as a mandatory dependency. No hardcoded user-facing strings. English (`en`) and Czech (`cs`) translation files are required deliverables. |
| **IV. Clean Architecture** | ✅ Pass | Frontend is the outermost Presentation layer. Feature-oriented folders separate API calls (`*Api.ts`), components, and shared utilities — no business logic in the UI components. |
| **V. Spec-First Development** | ✅ Pass | `spec.md` exists and is approved. This plan is derived from it. |

**Result**: All gates pass. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/002-web-ui-frontend/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-client.md    # Phase 1 output — full API contract
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
frontend/                          ← Vite + React SPA (sibling to src/)
├── public/
├── src/
│   ├── app/
│   │   ├── router.tsx             ← React Router v7 route tree
│   │   ├── providers.tsx          ← MantineProvider, QueryClientProvider, AuthProvider
│   │   └── theme.ts               ← Mantine custom theme + color scheme
│   │
│   ├── features/
│   │   ├── landing/
│   │   │   └── LandingPage.tsx    ← Public entry: "Create Tenant" / "Log In" CTAs
│   │   ├── auth/
│   │   │   ├── LoginPage.tsx
│   │   │   ├── AuthContext.tsx    ← Auth state + token refresh logic
│   │   │   ├── useAuth.ts
│   │   │   └── authApi.ts
│   │   ├── tenants/
│   │   │   ├── TenantRegisterPage.tsx
│   │   │   └── tenantsApi.ts
│   │   ├── courses/
│   │   │   ├── CourseListPage.tsx
│   │   │   ├── CourseCalendarPage.tsx
│   │   │   ├── CourseDetailPage.tsx
│   │   │   ├── CreateCoursePage.tsx
│   │   │   ├── EditCoursePage.tsx
│   │   │   ├── coursesApi.ts
│   │   │   └── types.ts
│   │   ├── registrations/
│   │   │   ├── CourseRosterPage.tsx
│   │   │   ├── CreateRegistrationModal.tsx
│   │   │   └── registrationsApi.ts
│   │   └── staff/
│   │       ├── StaffListPage.tsx
│   │       ├── CreateStaffModal.tsx
│   │       └── staffApi.ts
│   │
│   ├── shared/
│   │   ├── api/
│   │   │   └── client.ts          ← Typed fetch wrapper; injects auth headers; 401 retry
│   │   ├── components/
│   │   │   ├── AppShellLayout.tsx ← Mantine AppShell: nav + header + color toggle
│   │   │   ├── PageHeader.tsx
│   │   │   ├── ConfirmModal.tsx
│   │   │   └── StatusBadge.tsx
│   │   ├── hooks/
│   │   │   └── usePagination.ts
│   │   └── i18n/
│   │       ├── index.ts           ← i18next init; language detection
│   │       └── locales/
│   │           ├── en.json        ← English strings (mandatory)
│   │           └── cs.json        ← Czech strings (mandatory)
│   │
│   └── main.tsx
│
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

**Structure Decision**: Feature-oriented folders at `frontend/src/features/` mirror the backend's module-per-bounded-context organisation. The `shared/` folder holds cross-cutting infrastructure (API client, i18n, generic components). The `app/` folder is the bootstrapping layer only (router, providers, theme) — no feature logic goes here.

## Implementation Phases

### Phase 1: Project Bootstrap

Set up the Vite + React + Mantine project, configure routing skeleton, providers, theme, and i18n. At the end of this phase, the app shell renders with color scheme toggle and language switching. No real pages yet.

**Deliverables**:
- `frontend/` created with Vite template (`react-ts`)
- All dependencies installed (see quickstart.md)
- `app/theme.ts` — Mantine theme with light/dark support
- `app/providers.tsx` — MantineProvider + ColorScheme + QueryClient + AuthProvider stubs
- `app/router.tsx` — route skeleton with placeholder pages
- `shared/i18n/index.ts` — i18next configured with `en` and `cs` namespaces
- `shared/i18n/locales/en.json` and `cs.json` with keys for all planned strings
- `shared/api/client.ts` — base fetch wrapper with `Authorization` + `X-Tenant-Slug` headers
- `shared/components/AppShellLayout.tsx` — nav sidebar + header with color toggle

---

### Phase 2: Auth & Tenant Flows (P1 stories)

Implement the public landing page, tenant registration form, and login form with full token lifecycle management.

**Deliverables**:
- `features/landing/LandingPage.tsx` — two CTAs: "Create your workspace" + "Log in"
- `features/tenants/TenantRegisterPage.tsx` — Mantine form with fields: name, slug, language, admin username, email, password; calls `POST /api/v1/tenants`, then auto-logs-in
- `features/auth/AuthContext.tsx` — stores accessToken (memory), refreshToken + tenantSlug (localStorage); provides `login()`, `logout()`, `refreshSession()`
- `features/auth/LoginPage.tsx` — form with tenant slug, username, password; on success → `/app/courses`
- `features/auth/authApi.ts` — `login()`, `refreshToken()` functions
- Route guard: unauthenticated users visiting `/app/*` redirect to `/login`
- Silent refresh on app startup (before route render)

---

### Phase 3: Course List & Calendar (P2 story — read)

Implement the main courses view with list and calendar toggle.

**Deliverables**:
- `features/courses/coursesApi.ts` — `listCourses()`, `getCourse()`, `cancelCourse()`
- `features/courses/CourseListPage.tsx` — table/card list using TanStack Query; shows title, type, status badge, session count, first session date; "View" link to detail; "New Course" button; calendar toggle
- `features/courses/CourseCalendarPage.tsx` — monthly grid derived from `listCourses()` data; sessions rendered as colored badges on their date; click navigates to course detail
- `features/courses/types.ts` — TypeScript types matching data-model.md
- Translation keys for all labels, statuses, and empty states

---

### Phase 4: Course Create & Edit (P2 story — write)

Implement course creation and editing forms.

**Deliverables**:
- `features/courses/CreateCoursePage.tsx` — Mantine form: title, description, courseType (select), registrationMode (select), capacity (number); dynamic session list (add/remove rows with scheduledAt, durationMinutes, location); submits to `POST /api/v1/courses`; on success → course detail page
- `features/courses/EditCoursePage.tsx` — pre-populated Mantine form for `PATCH /api/v1/courses/:id`; available fields: title, description, capacity, registrationMode
- Cancel course button in course detail → confirms via `ConfirmModal` → calls `POST /api/v1/courses/:id/cancel`
- Validation: future-date enforcement on session scheduledAt inputs
- TanStack Query cache invalidation after mutations

---

### Phase 5: Course Detail & Sessions

Implement the course detail page with session list.

**Deliverables**:
- `features/courses/CourseDetailPage.tsx` — full course info; sessions list (sequence, date/time, duration, location, ends at); navigation to Edit + Roster; Cancel Course action (Admin)
- Session list renders with formatted local date/time
- Link to `CourseRosterPage` for each course

---

### Phase 6: Registrations (P3 story)

Implement registration roster view and create/cancel registration.

**Deliverables**:
- `features/registrations/registrationsApi.ts` — `getRoster()`, `createRegistration()`, `cancelRegistration()`
- `features/registrations/CourseRosterPage.tsx` — paginated table of registrations (name, email, source, status, date); "Add Registration" button opens modal; cancel action with confirm
- `features/registrations/CreateRegistrationModal.tsx` — Mantine modal with `participantName`, `participantEmail` fields; 409 capacity error surfaced inline
- Translation keys for all registration states and actions

---

### Phase 7: Staff Management (P4 story)

Implement staff user list and create/deactivate flows (Admin only).

**Deliverables**:
- `features/staff/staffApi.ts` — `listStaff()`, `createStaff()`, `deactivateStaff()`
- `features/staff/StaffListPage.tsx` — table of staff users; role + status badges; "Deactivate" action (confirm modal); "Add Staff" button; hidden from non-Admin users
- `features/staff/CreateStaffModal.tsx` — username, email, password, role (select Admin/Staff) form
- Nav sidebar item for "Staff" visible to Admin role only
- 403 responses handled gracefully if non-Admin navigates directly

---

### Phase 8: Polish & Error Handling

**Deliverables**:
- Global error boundary catches unexpected React errors
- All API errors surface as Mantine notifications (top-right toast)
- 422 validation errors mapped to form field inline errors
- Loading skeletons on all data-fetching pages
- Empty states for courses list, registration roster, staff list
- Route 404 page
- Page `<title>` updates per route

## Complexity Tracking

> No constitution violations. No complexity justification needed.
