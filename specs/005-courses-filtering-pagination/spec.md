# Feature Specification: Courses Page Filtering, Sorting & Temporal Views

**Feature Branch**: `005-courses-filtering-pagination`  
**Created**: 2026-04-03  
**Status**: Draft  
**Input**: User description: "Courses page in frontend does not allow any filtering, sorting, pagination, etc of courses, also it does not allow distingiush between courses in the past and ongoing and future, everything is active or deleted... Improve the page, also you can add filtering by some categories etc. tags ..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Temporal Course Classification (Priority: P1)

A staff member visits the Courses page and immediately wants to see only the courses that are currently running (ongoing), those that haven't started yet (upcoming), or those that have already ended (past). Right now all courses are shown in one flat list making it impossible to quickly assess what is actively happening.

**Why this priority**: This is the most requested orientation signal — staff need to know at a glance whether their focus should be on upcoming preparation, active delivery, or historical review. It works entirely with existing data.

**Independent Test**: Navigate to the Courses page; four clearly labelled tabs or filter buttons ("All", "Upcoming", "Ongoing", "Past") appear; clicking each shows only the courses matching that temporal window.

**Acceptance Scenarios**:

1. **Given** courses exist with first sessions in the future, **When** the user selects "Upcoming", **Then** only courses whose first session is in the future are shown.
2. **Given** courses exist with sessions that have started but not all ended, **When** the user selects "Ongoing", **Then** only those courses are listed.
3. **Given** courses that have ended or hold Completed/Cancelled status, **When** the user selects "Past", **Then** only those courses appear.
4. **Given** any temporal filter is active, **When** the user selects "All", **Then** every course is shown regardless of timing.

---

### User Story 2 - Text Search and Status Filtering (Priority: P2)

A staff member is looking for a specific course by name or wants to narrow the list to only "Draft" courses before a publication push. They need a search box and status filter without leaving the page.

**Why this priority**: Text search is the fastest navigation shortcut for large course catalogues; status filtering completes the core filtering set and works entirely with existing data.

**Independent Test**: Type part of a course title in the search field; the table updates to show only matching courses. Select "Draft" from a status filter; only draft courses remain visible.

**Acceptance Scenarios**:

1. **Given** a list of courses, **When** the user types "yoga" in the search box, **Then** only courses whose title contains "yoga" (case-insensitive) are shown.
2. **Given** courses with statuses Draft, Active, Cancelled, Completed, **When** the user selects "Draft" in the status filter, **Then** only Draft courses appear.
3. **Given** both a search term and a status filter are applied, **When** either changes, **Then** results reflect both filters simultaneously.
4. **Given** filters are applied, **When** the user clicks "Clear all", **Then** the full list is restored.

---

### User Story 3 - Sortable Columns (Priority: P3)

A staff member wants to sort courses by their first session date, title alphabetically, or capacity. Currently the list order is fixed.

**Why this priority**: Sorting helps prioritise which courses to act on first; date-based sorting is especially valuable when combined with temporal filters.

**Independent Test**: Click the "First Session" column header once; courses sort ascending by date. Click again; they sort descending. A visual indicator shows current sort direction.

**Acceptance Scenarios**:

1. **Given** the course list is visible, **When** the user clicks a sortable column header, **Then** the list re-sorts in ascending order by that column and an indicator appears.
2. **Given** a column is already sorted ascending, **When** the user clicks the same header again, **Then** the list re-sorts in descending order.
3. **Given** a sort is applied, **When** the user switches temporal tabs, **Then** sort order is preserved within the new view.

---

### User Story 4 - Pagination (Priority: P4)

A staff member with many courses (50+) needs to navigate through pages instead of scrolling an endlessly long table.

**Why this priority**: Essential for performance and usability at scale, but only meaningful once there are many courses.

**Independent Test**: With more than 25 visible courses, pagination controls appear at the bottom; clicking "Next" advances to the second page.

**Acceptance Scenarios**:

1. **Given** more courses exist than the page size limit, **When** the page loads, **Then** only one page of results is shown and pagination controls appear.
2. **Given** pagination controls are visible, **When** the user clicks a page number, **Then** the corresponding page of courses is displayed.
3. **Given** the user is on page 2 and applies a filter that reduces results to fewer than one page, **When** the filter is applied, **Then** the view resets to page 1 and pagination controls are hidden.

---

### User Story 5 - Course Type and Tag Filtering (Priority: P5)

A staff member wants to quickly narrow down courses by type (One-Time vs. Multi-Session) or by tags/categories associated with courses (e.g., "Yoga", "Advanced", "Online").

**Why this priority**: Complements text search for organised catalogues; most impactful once a tagging system exists on courses.

**Independent Test**: Select "Multi-Session" from the course type filter; only multi-session courses appear. If tags exist, selecting a tag narrows results to matching courses.

**Acceptance Scenarios**:

1. **Given** courses of both types exist, **When** the user filters by "One-Time", **Then** only one-time courses are listed.
2. **Given** courses have tags, **When** the user selects a tag, **Then** only courses bearing that tag are shown.
3. **Given** no tags exist in the system, **When** the user views the filter panel, **Then** the tag filter is not shown (hidden rather than empty).

---

### Edge Cases

- What happens when filters produce zero results? → An empty state message is shown with a "Clear filters" action.
- What happens when a course has no sessions (no first session date)? → It is classified as "Upcoming" by default and sorts last when sorting by date ascending.
- What happens when the backend does not yet support server-side filtering? → Filtering, sorting, and pagination are applied client-side on the full list returned by the existing API.
- How does pagination behave when the user changes a filter? → The view always resets to page 1 when any filter or sort criterion changes.
- What if a course's first session is today — is it Ongoing or Upcoming? → A course is Ongoing once its first session has started (current time ≥ first session start time).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a temporal view selector ("All", "Upcoming", "Ongoing", "Past") on the Courses list page that filters courses by their lifecycle timing relative to the current date and time.
- **FR-002**: System MUST classify a course as "Upcoming" if its first session is in the future, "Ongoing" if it has started but not yet ended (last session end time has not passed), and "Past" if it has ended or holds a Completed or Cancelled status.
- **FR-003**: System MUST provide a real-time text search field that filters the course list by course title (case-insensitive partial match).
- **FR-004**: System MUST provide a status multi-select filter covering statuses: Draft, Active, Cancelled, Completed.
- **FR-005**: System MUST allow sorting by at least: Title (alphabetical), First Session (date), and Capacity (numeric).
- **FR-006**: Sortable column headers MUST toggle between ascending and descending order on repeated clicks and visually indicate the current sort direction with an icon or marker.
- **FR-007**: System MUST paginate the course list when visible results exceed 25 items, with pagination controls at the bottom of the list.
- **FR-008**: Pagination MUST reset to page 1 whenever any filter or sort criterion changes.
- **FR-009**: System MUST provide a course type filter (One-Time / Multi-Session).
- **FR-010**: If courses have tags associated with them, the system MUST provide a tag multi-select filter; if no tags exist, the tag filter MUST be hidden.
- **FR-011**: When active filters return zero results, the system MUST display an informative empty state with a "Clear filters" action that removes all active filters.
- **FR-012**: All active filters MUST be clearable individually and collectively via a single "Clear all" control that is visible whenever any filter is active.
- **FR-013**: Default sort order (no user selection) MUST be first session date ascending (soonest first).

### Key Entities

- **Course**: Primary entity; title, type (OneTime/MultiSession), status (Draft/Active/Cancelled/Completed), capacity, session count, first session date.
- **CourseSession**: Individual scheduled sessions whose dates collectively determine temporal classification.
- **CourseTag** *(new or future)*: A label or category associated with a course, used for grouping and filtering.
- **TemporalBucket**: A derived classification (Upcoming / Ongoing / Past) computed from session dates relative to the current time — not stored, calculated on the fly.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff can locate a specific course by name within 10 seconds using the search field.
- **SC-002**: Staff can isolate all currently running (Ongoing) courses in fewer than 3 interactions from the Courses page.
- **SC-003**: Filter results appear within 1 second of any filter change (including a visible loading state if data is fetched remotely).
- **SC-004**: No single page view of the course list contains more than 25 items when more than 25 courses match the active filters.
- **SC-005**: Zero courses are incorrectly classified into the wrong temporal bucket (Upcoming/Ongoing/Past) based on their session dates.
- **SC-006**: Courses with no session date are never shown under "Ongoing" or "Past" temporal views.

## Assumptions

- Tags/categories are not yet stored on courses in the backend; tag filtering is deferred to a follow-up backend change and the UI hides tag filter until tags are present.
- The existing `firstSessionAt` field is used for "Upcoming" classification; last session end time (`lastSessionEndsAt`) will need to be added to the `CourseListItem` API response to accurately determine "Ongoing" vs "Past" — or this classification falls back to course status.
- Filtering, sorting, and pagination are implemented client-side initially, operating on the full course list returned by the existing `/api/v1/courses/` endpoint. Server-side support is a future optimization.
- The course list is used exclusively by authenticated staff members; no public-facing filtering is in scope.
- The calendar view is out of scope for filtering/sorting changes in this feature; filters apply to the list view only.
- Page size of 25 items is fixed (not user-configurable) in this version.
- Registration mode (Open / StaffOnly) is not included as a filter criterion in v1 but may be added later.
