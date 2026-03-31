# Quickstart: Web UI Frontend

**Branch**: `002-web-ui-frontend` | **Date**: 2026-03-31

---

## Prerequisites

- Node.js 22+ (LTS)
- pnpm 9+ (or npm 10+)
- Backend running on `http://localhost:5000` (via `cd src/Terminar.AppHost && dotnet run`)

---

## Setup

```bash
cd frontend
pnpm install
```

Create a `.env.local` file:

```env
VITE_API_BASE_URL=http://localhost:5000
```

---

## Run Development Server

```bash
pnpm dev
# Opens at http://localhost:5173
```

---

## Build for Production

```bash
pnpm build
pnpm preview   # preview production build locally
```

---

## Project Structure

```
frontend/
├── public/                     ← static assets
├── src/
│   ├── app/
│   │   ├── router.tsx          ← React Router route definitions
│   │   ├── providers.tsx       ← MantineProvider, QueryClientProvider, AuthProvider
│   │   └── theme.ts            ← Mantine theme + color scheme config
│   │
│   ├── features/
│   │   ├── landing/
│   │   │   └── LandingPage.tsx
│   │   │
│   │   ├── auth/
│   │   │   ├── LoginPage.tsx
│   │   │   ├── useAuth.ts      ← auth context hook
│   │   │   ├── AuthContext.tsx
│   │   │   └── authApi.ts      ← login, refresh API calls
│   │   │
│   │   ├── tenants/
│   │   │   ├── TenantRegisterPage.tsx
│   │   │   └── tenantsApi.ts
│   │   │
│   │   ├── courses/
│   │   │   ├── CourseListPage.tsx
│   │   │   ├── CourseCalendarPage.tsx
│   │   │   ├── CourseDetailPage.tsx
│   │   │   ├── CreateCoursePage.tsx
│   │   │   ├── EditCoursePage.tsx
│   │   │   ├── coursesApi.ts
│   │   │   └── types.ts
│   │   │
│   │   ├── registrations/
│   │   │   ├── CourseRosterPage.tsx
│   │   │   ├── CreateRegistrationModal.tsx
│   │   │   └── registrationsApi.ts
│   │   │
│   │   └── staff/
│   │       ├── StaffListPage.tsx
│   │       ├── CreateStaffModal.tsx
│   │       └── staffApi.ts
│   │
│   ├── shared/
│   │   ├── api/
│   │   │   └── client.ts       ← fetch wrapper with auth headers + 401 retry
│   │   ├── components/
│   │   │   ├── AppShellLayout.tsx
│   │   │   ├── PageHeader.tsx
│   │   │   ├── ConfirmModal.tsx
│   │   │   └── StatusBadge.tsx
│   │   ├── hooks/
│   │   │   └── usePagination.ts
│   │   └── i18n/
│   │       ├── index.ts        ← i18next setup
│   │       └── locales/
│   │           ├── en.json
│   │           └── cs.json
│   │
│   └── main.tsx                ← app entry point
│
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

---

## Key Libraries

| Library | Version | Purpose |
|---------|---------|---------|
| `react` | 19.x | UI framework |
| `@mantine/core` | 9.x | Component library |
| `@mantine/hooks` | 9.x | `useColorScheme`, form helpers |
| `@mantine/dates` | 9.x | Date pickers, calendar grid |
| `@mantine/notifications` | 9.x | Toast notifications |
| `@mantine/form` | 9.x | Form state + validation |
| `react-router` | 7.x | Client-side routing |
| `@tanstack/react-query` | 5.x | Server state / data fetching |
| `i18next` | latest | i18n framework |
| `react-i18next` | latest | React bindings for i18n |
| `dayjs` | latest | Date formatting (Mantine peer dep) |

---

## Adding a New Language

1. Create `src/shared/i18n/locales/{lang}.json` (copy from `en.json`).
2. Add the language to `src/shared/i18n/index.ts` resources map.
3. Add a language switcher option in the app header if desired.

---

## Authentication Notes

- On app load, `AuthContext` checks `localStorage` for a saved refresh token.
- If found, it calls `POST /api/v1/auth/refresh` silently before rendering protected routes.
- If no refresh token or refresh fails, the user lands at `/login`.
- The slug entered during login is stored in `localStorage` as `tenantSlug` and sent as `X-Tenant-Slug` on every API call.
