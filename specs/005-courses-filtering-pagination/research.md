# Research: Courses Filtering, Sorting & Temporal Views

## Decision 1: Client-side vs. Server-side Filtering

**Decision**: Client-side filtering on the full list for this feature iteration.

**Rationale**: The existing `GET /api/v1/courses/` endpoint returns all tenant courses in a single call. At the current scale (typical tenant has <200 courses), loading the full list and filtering in the browser is fast and avoids backend query complexity. The `usePagination` hook already exists (`frontend/src/shared/hooks/usePagination.ts`), confirming the team has done client-side pagination before.

**Alternatives considered**: Server-side filtering via query parameters would be more scalable but requires non-trivial backend changes (EF Core LINQ filter composition, pagination contracts, test coverage). Deferred to a future optimization.

---

## Decision 2: Source of `LastSessionEndsAt` for Temporal Classification

**Decision**: Add `LastSessionEndsAt` (`DateTime?`) to `CourseListItem` DTO, computed as `c.Sessions.MaxBy(s => s.ScheduledAt)?.EndsAt` in `ListCoursesHandler`. `Session.EndsAt` is already a computed property (`ScheduledAt.AddMinutes(DurationMinutes)`).

**Rationale**: Temporal classification requires knowing when the last session ends (to distinguish "Ongoing" from "Past"). `FirstSessionAt` alone is insufficient — a course started last year but still running needs to show as "Ongoing". The backend domain model already provides `EndsAt` on `Session`.

**Alternatives considered**: Deriving classification purely from `CourseStatus` (Completed/Cancelled → Past, Active → Ongoing/Upcoming based on first session). Rejected because status is manually managed and may lag behind actual session timing — a course can be "Active" but all sessions finished.

---

## Decision 3: Tag Source for Tag Filtering

**Decision**: Expose `ExcusalPolicy.Tags` from the course domain entity as `Tags` in the `CourseListItem` DTO. These tags already exist as `List<string>` on `CourseExcusalPolicy` and represent course categorization (e.g., "yoga", "advanced").

**Rationale**: Tags already exist in the system. Exposing them in the list DTO is a minimal change. The tag filter UI only renders when at least one course has tags (discovered at runtime from the loaded data), matching FR-010.

**Alternatives considered**: Introducing a separate `CourseTag` entity with its own table and management UI. Overkill for v1 — the existing excusal policy tags serve the same categorization purpose.

---

## Decision 4: Temporal Bucket Logic

**Decision**: Temporal classification algorithm (client-side, in UTC):

| Bucket   | Condition                                                                                   |
|----------|---------------------------------------------------------------------------------------------|
| Upcoming | `firstSessionAt` is null OR `firstSessionAt > now`                                          |
| Ongoing  | `firstSessionAt <= now` AND (`lastSessionEndsAt` is null OR `lastSessionEndsAt > now`)       |
| Past     | `lastSessionEndsAt <= now` OR status is `Cancelled` or `Completed`                          |
| All      | No filter                                                                                   |

Courses with no sessions are treated as "Upcoming" (they have no sessions yet scheduled).

**Rationale**: This reflects actual delivery state rather than the manually-managed `CourseStatus` field, giving staff accurate real-time classification. Cancelled/Completed status overrides into "Past" regardless of dates (a cancelled future course is past-tense from an administrative perspective).

---

## Decision 5: Default Sort Order

**Decision**: Default sort is first session date ascending (soonest first), with courses having no sessions sorted last.

**Rationale**: Staff most naturally want to see upcoming courses first. This matches the temporal "Upcoming" default and reduces clicks for the common case.

---

## Decision 6: Pagination Page Size

**Decision**: 25 items per page (as stated in spec), overriding the existing `usePagination` hook default of 20.

**Rationale**: The spec explicitly calls for 25. The `usePagination` hook accepts `defaultPageSize` as a parameter.

---

## Decision 7: Filter State Management

**Decision**: All filter state lives in a dedicated `useCoursesFilter` custom hook (co-located in `frontend/src/features/courses/`). No URL-based persistence for v1.

**Rationale**: Encapsulates the filter logic cleanly, keeps `CourseListPage` lean, and is easily testable. URL persistence would require React Router integration and was not requested — deferred.

---

## No Backend Changes Required for Search/Filter/Sort/Pagination

All search, filter, sort, and pagination logic runs client-side. The only backend change is extending `CourseListItem` to add two new fields: `LastSessionEndsAt` and `Tags`.
