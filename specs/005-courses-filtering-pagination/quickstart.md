# Quickstart: Courses Filtering, Sorting & Temporal Views

## Prerequisites

- .NET 10 SDK, Node.js 22+
- Docker (for PostgreSQL via Aspire)
- Existing dev environment from feature 001–004 set up

## Run the App

```bash
# Start backend (PostgreSQL auto-started by Aspire)
cd src/Terminar.AppHost && dotnet run

# Start frontend dev server (separate terminal)
cd frontend && npm run dev
```

## Verify the Change Works

1. Log in as a staff user and navigate to **Courses**.
2. Confirm four temporal tabs appear: **All / Upcoming / Ongoing / Past**.
3. Click "Upcoming" — only future courses show.
4. Type a partial course title in the search box — list filters in real time.
5. Select a status from the status filter — list narrows accordingly.
6. Click the **First Session** column header — list re-sorts, direction indicator appears.
7. Click again — direction reverses.
8. If more than 25 courses exist, verify pagination controls appear at the bottom.

## Manual Testing Data Setup

To exercise all temporal buckets, create:
- A course with a session in the **future** (Upcoming).
- A course with a session that **started today** and ends later today (Ongoing).
- A course where all sessions have **already ended** (Past via date), or one with status `Completed` or `Cancelled`.

## Backend Change (minimal)

Only `ListCoursesQuery.cs` and `ListCoursesHandler.cs` are changed — no migrations, no new tables.

```bash
# Build the backend to verify
cd src && dotnet build
```

## Frontend Change Overview

Files changed:
- `frontend/src/features/courses/types.ts` — add `lastSessionEndsAt`, `tags`
- `frontend/src/features/courses/coursesApi.ts` — no change needed (same endpoint)
- `frontend/src/features/courses/hooks/useCoursesFilter.ts` — new hook (filter state + logic)
- `frontend/src/features/courses/CourseListPage.tsx` — add filter UI, sortable headers, pagination
- `frontend/src/shared/i18n/locales/en.json` + `cs.json` — add new i18n keys

```bash
# Run frontend type-check
cd frontend && npm run typecheck
```
