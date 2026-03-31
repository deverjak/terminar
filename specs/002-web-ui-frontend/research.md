# Research: Web UI Frontend

**Branch**: `002-web-ui-frontend` | **Date**: 2026-03-31

## Decisions

---

### 1. UI Framework & Version

**Decision**: React 19 + Mantine v9 (as specified by user)

**Rationale**: Mantine v9 is the latest stable major version. It ships with a complete component suite (forms, modals, tables, notifications, date pickers, menus) that covers all required UI patterns without additional libraries. React 19 is the current stable release.

**Alternatives considered**: Next.js (adds SSR complexity not needed for an SPA admin tool), plain CSS (too much from-scratch work), Ant Design (heavier and less composable).

---

### 2. Routing

**Decision**: React Router v7 (latest)

**Rationale**: React Router v7 (formerly Remix router) is the current stable release and the de-facto standard for React SPAs. It supports browser history navigation (back/forward work correctly), nested layouts (shared AppShell wrapper), and typed route params.

**Alternatives considered**: TanStack Router (excellent but adds learning curve); Next.js App Router (SSR overhead not needed).

---

### 3. Light/Dark Theme

**Decision**: Mantine's built-in `ColorSchemeScript` + `MantineProvider` with `colorScheme` managed via `@mantine/hooks` `useColorScheme`, stored in `localStorage` to persist user preference.

**Rationale**: Mantine v9 natively supports light/dark mode through its theming system. No extra library needed. The toggle can live in the AppShell header and persists across sessions.

---

### 4. Folder Structure

**Decision**: Feature-oriented folders under `src/features/`, with shared infrastructure in `src/shared/` and app bootstrapping in `src/app/`.

```
src/
  app/           ← router, providers, global theme
  features/
    landing/     ← landing page (unauthenticated)
    auth/        ← login form, token management
    tenants/     ← tenant creation form
    courses/     ← list, calendar, detail, create, edit
    registrations/ ← roster view, create/cancel registration
    staff/       ← staff user list, create, deactivate
  shared/
    api/         ← typed API client (fetch wrapper)
    components/  ← generic UI (PageHeader, ConfirmModal, etc.)
    i18n/        ← translation files and i18n setup
    hooks/       ← shared hooks (useAuth, useTenant, etc.)
```

**Rationale**: Feature folders keep all related files (components, hooks, API calls, types) co-located. This matches the user's explicit requirement and mirrors the backend's module-per-bounded-context pattern.

---

### 5. i18n (Internationalisation)

**Decision**: `react-i18next` with `i18next`, with translation files per language in `src/shared/i18n/locales/`. Minimum two languages: English (`en`) and Czech (`cs`) — consistent with Termínář's target market.

**Rationale**: The constitution (Principle III) mandates that **all user-facing content MUST be internationalizable** and that no hardcoded user-facing strings are permitted. This is non-negotiable for every layer including the frontend. `react-i18next` is the industry standard, integrates trivially with React hooks (`useTranslation`), and requires no build-time changes.

**Alternatives considered**: `FormatJS/react-intl` (more complex setup, same result), hardcoded strings (BLOCKED by constitution).

---

### 6. JWT Token Management

**Decision**: Access token stored in memory (React state / Zustand store); refresh token stored in `localStorage`. On page reload, the app attempts a silent refresh using the persisted refresh token before rendering protected routes.

**Rationale**: Storing the access token only in memory limits XSS exposure. The refresh token in localStorage is the standard trade-off for SPAs (cookie-based would require CORS + cookie config). A refresh is attempted at app startup to restore session after reload.

**How refresh works**:
1. At app load: if `refreshToken` exists in localStorage, call `POST /api/v1/auth/refresh` with stored `userId` + `refreshToken`.
2. On 401 response to any authenticated request: automatically attempt refresh and retry once.
3. If refresh fails: clear stored tokens and redirect to `/login`.

---

### 7. Tenant Context in API Requests

**Decision**: Pass the tenant slug as the `X-Tenant-Slug` request header on every authenticated API call. The `TenantResolutionMiddleware` (already implemented in backend, observation 147) reads this header to resolve the current tenant.

**Rationale**: The backend's `TenantResolutionMiddleware` was updated to support slug-based lookup. Passing it as a header is clean, doesn't pollute URL paths, and is consistent across all requests. The slug is stored in the auth context after login.

**Login flow**:
1. User enters slug + username + password on the Login page.
2. Frontend calls `POST /api/v1/auth/login` with `X-Tenant-Slug: {slug}` header and `{ username, password }` body.
3. Backend resolves tenant from header, authenticates user, returns tokens.
4. Frontend stores slug in auth context (alongside tokens) for all subsequent requests.

---

### 8. Calendar Component

**Decision**: Use Mantine's `@mantine/dates` package (ships with Mantine v9) for date display. For the calendar/event view of sessions, use a lightweight custom approach: a monthly grid where sessions appear as colored badges on their date. Full calendar interaction (drag-drop) is out of scope.

**Rationale**: A full-featured calendar library (FullCalendar, react-big-calendar) adds significant bundle weight. Mantine's date utilities cover the grid layout. Sessions are read-only in the calendar view (click to navigate to course detail), which keeps the implementation simple.

---

### 9. State Management

**Decision**: React context + `useState`/`useReducer` for auth state; TanStack Query v5 for server state (API data caching, loading/error states, background refetch).

**Rationale**: TanStack Query eliminates manual loading/error handling boilerplate and gives automatic cache invalidation (e.g., after creating a course, the course list cache is invalidated). Auth state is small enough to live in a Context without a full store library.

**Alternatives considered**: Zustand (good, but TanStack Query alone handles the server state need); Redux Toolkit (overkill for this scope).

---

### 10. Frontend Project Location

**Decision**: Create the frontend as a Vite-based project at `frontend/` in the repository root (sibling to `src/` which contains the .NET backend).

**Rationale**: Keeps the frontend separate from the .NET solution while living in the same repo. The backend and frontend are independently runnable. Vite is the fastest build tool for React development with HMR.

---

## Resolved Clarifications

| Original Unknown | Resolution |
|------------------|------------|
| Password policy during tenant creation | User explicitly stated: user selects password during tenant registration. No auto-generation. |
| Mobile support | Out of scope (spec assumption confirmed). |
| i18n requirement | Required by constitution (Principle III). `react-i18next` selected. Minimum en + cs. |
| Tenant identification in API calls | `X-Tenant-Slug` header, resolved by TenantResolutionMiddleware. |
| Calendar library | Mantine dates + custom monthly grid (no heavy calendar library). |
