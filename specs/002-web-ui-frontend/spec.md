# Feature Specification: Web UI Frontend

**Feature Branch**: `002-web-ui-frontend`
**Created**: 2026-03-31
**Status**: Draft
**Input**: User description: "Landing page, create tenant or log in using existing id. After log in, you should be able to see existing courses including calendar view. You should be able to add new course. Support all relevant other endpoint in gui."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tenant Onboarding via Landing Page (Priority: P1)

A new organization visits the application for the first time. They see a welcoming landing page that clearly explains what the system is for and presents two options: create a new tenant account or log in to an existing one. They choose to create a new tenant, fill in organization details, and are automatically logged in as the first admin user.

**Why this priority**: Without onboarding, no users can enter the system. This is the entry point for every new customer and is required for any other flow to work.

**Independent Test**: A fresh visitor can open the app, click "Create Tenant", fill in a name and slug, submit, and land on the authenticated dashboard — delivering a fully functional onboarding flow.

**Acceptance Scenarios**:

1. **Given** a visitor arrives at the root URL with no session, **When** they view the page, **Then** they see a landing page with "Create Tenant" and "Log In" options prominently displayed.
2. **Given** a visitor clicks "Create Tenant", **When** they submit valid organization details (name, slug), **Then** a tenant is created, an initial admin account is provisioned, and they are redirected to the authenticated dashboard.
3. **Given** a visitor submits a tenant slug that is already taken, **When** the form is submitted, **Then** an inline error message explains the slug is unavailable and the form remains editable.
4. **Given** a visitor submits the tenant creation form with missing required fields, **When** the form is submitted, **Then** validation errors appear next to each missing field without page reload.

---

### User Story 2 - Staff Login to Existing Tenant (Priority: P1)

A staff member with existing credentials opens the app, selects "Log In", enters their tenant identifier and credentials, and lands on the authenticated dashboard.

**Why this priority**: Returning users make up the majority of daily traffic. Login must work reliably before any other authenticated feature is useful.

**Independent Test**: An existing staff user can enter their email, password, and tenant slug, submit, and reach the course list — delivering authenticated access independently of other features.

**Acceptance Scenarios**:

1. **Given** a visitor clicks "Log In" on the landing page, **When** they enter a valid tenant slug, email, and password, **Then** they are authenticated and redirected to the course list view.
2. **Given** a staff user submits incorrect credentials, **When** the login form is submitted, **Then** a clear error message is shown and no redirect occurs.
3. **Given** a staff user's account is deactivated, **When** they attempt to log in, **Then** they receive an informative message explaining their account is inactive.
4. **Given** an authenticated user's session has expired, **When** they perform any action, **Then** the session is silently refreshed using the refresh token, or they are redirected to the login page if the refresh token is also expired.

---

### User Story 3 - Course List and Calendar View (Priority: P2)

After login, a staff member can browse all courses for their tenant in two views: a list view showing course names, status, and capacity, and a calendar view showing scheduled sessions visually across dates.

**Why this priority**: Viewing existing courses is the primary read operation for day-to-day use. It provides immediate value after the login flow is complete.

**Independent Test**: An authenticated user navigates to Courses and can switch between list and calendar views, seeing all courses and their sessions — fully independently testable once login works.

**Acceptance Scenarios**:

1. **Given** an authenticated user navigates to the Courses section, **When** the page loads, **Then** they see a list of all courses belonging to their tenant with name, status, and session count visible.
2. **Given** a user is on the course list, **When** they switch to calendar view, **Then** course sessions appear as events on a monthly/weekly calendar at their scheduled dates.
3. **Given** a tenant has no courses yet, **When** an authenticated user views the courses section, **Then** an empty state message is shown with a prompt to create the first course.
4. **Given** a user clicks on a course in either view, **When** the detail opens, **Then** they see full course details including all scheduled sessions, capacity, and current registration count.

---

### User Story 4 - Create New Course (Priority: P2)

An authenticated staff member fills in a form to create a new course, specifying its name, description, capacity, and optionally adding sessions.

**Why this priority**: Course creation is the primary write operation for staff users. It directly enables the registration workflow downstream.

**Independent Test**: An authenticated user completes the "Add Course" form with name, description, and capacity, submits, and the new course appears in the list — testable independently of session or registration management.

**Acceptance Scenarios**:

1. **Given** an authenticated user clicks "Add Course", **When** they fill in name, description, and max capacity and submit, **Then** the course is created and appears in the course list.
2. **Given** a user submits the course form with missing required fields, **When** validation runs, **Then** inline error messages highlight each missing field.
3. **Given** a newly created course exists, **When** the user adds a session with a future date and time, **Then** the session appears on the course detail and in the calendar view.
4. **Given** a user attempts to schedule a session in the past, **When** they submit, **Then** an error message indicates sessions must be scheduled in the future.

---

### User Story 5 - Course Session Management (Priority: P3)

A staff member can add, view, and manage sessions within an existing course — setting dates, times, and instructor details.

**Why this priority**: Session management is essential for the calendar view to be meaningful, but depends on courses existing first.

**Independent Test**: An authenticated user opens a course, adds a session with a future date and time, and sees it appear in the course detail and calendar.

**Acceptance Scenarios**:

1. **Given** an authenticated user is on a course detail page, **When** they add a new session with a valid future date and time, **Then** the session is saved and displayed in the session list.
2. **Given** a course has sessions, **When** the user views the calendar, **Then** each session appears on its scheduled date as a clickable event.

---

### User Story 6 - Registration Management (Priority: P3)

A staff member can view registrations for a course session, see who is registered, and manually add or manage registrations.

**Why this priority**: Registrations represent the core business value of the system, but depend on courses and sessions existing first.

**Independent Test**: An authenticated user navigates to a course session's registrations, sees the list of registered participants, and can add a new registration — independently testable given a course and session exist.

**Acceptance Scenarios**:

1. **Given** a course session exists, **When** an authenticated user views the session registrations, **Then** a list of registered participants is shown with their details.
2. **Given** a session has available capacity, **When** a staff member submits a registration for a participant, **Then** the registration is recorded and the available slot count decreases.
3. **Given** a session is at full capacity, **When** a staff member attempts to add a registration, **Then** an error message indicates the session is full.

---

### User Story 7 - Staff User Management (Priority: P4)

An admin staff user can view the list of staff users in their tenant, invite new users, and deactivate existing ones.

**Why this priority**: User management is an administrative function that is important but not critical for the core course management workflow.

**Independent Test**: An authenticated admin user navigates to the Staff section, sees the list of staff accounts, and can deactivate a user — independently testable given login works.

**Acceptance Scenarios**:

1. **Given** an admin user navigates to Staff Management, **When** the page loads, **Then** a list of all staff users in the tenant is shown with name, email, and active status.
2. **Given** an admin user clicks "Deactivate" on a staff account, **When** they confirm the action, **Then** the account is deactivated and the user can no longer log in.
3. **Given** an admin user creates a new staff user with a name and email, **When** submitted successfully, **Then** the new user appears in the staff list.

---

### Edge Cases

- What happens when an authenticated user's tenant becomes invalid mid-session?
- How does the calendar handle sessions spanning midnight or lasting multiple hours?
- What happens when a user navigates to a course that belongs to a different tenant (unauthorized access attempt)?
- How does the system display courses when there are more than 50+ courses (pagination or infinite scroll)?
- What happens when the backend is temporarily unreachable — are errors surfaced clearly?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST display a landing page accessible without authentication, with clearly labeled options to create a new tenant or log in.
- **FR-002**: The tenant creation form MUST collect organization name and a unique slug identifier, and MUST display inline validation errors for missing or conflicting inputs.
- **FR-003**: Upon successful tenant creation, the system MUST automatically authenticate the user as the initial admin and redirect to the authenticated dashboard.
- **FR-004**: The login form MUST accept tenant slug, email, and password, authenticate against the backend, and redirect to the course list on success.
- **FR-005**: The authenticated session MUST be maintained across page reloads and MUST silently refresh using a refresh token when the access token expires.
- **FR-006**: The Courses section MUST display all courses for the authenticated user's tenant in a list view with name, status, and session count.
- **FR-007**: The Courses section MUST provide a calendar view showing course sessions as events on their scheduled dates, navigable by month and week.
- **FR-008**: Users MUST be able to create a new course by providing a name, description, and maximum capacity.
- **FR-009**: Users MUST be able to add sessions to an existing course, specifying a future date and time; past dates MUST be rejected with a clear error.
- **FR-010**: Users MUST be able to view all registrations for a course session, including participant details and available capacity.
- **FR-011**: Users MUST be able to create a new registration for a session that has available capacity.
- **FR-012**: Admin users MUST be able to view the staff user list, create new staff users, and deactivate existing ones.
- **FR-013**: All authenticated pages MUST redirect unauthenticated users to the login page.
- **FR-014**: All API error responses MUST be surfaced to the user through clear, human-readable in-app messages.
- **FR-015**: The application MUST scope all data displayed to the authenticated user's tenant only.

### Key Entities

- **Tenant**: An organization account identified by a unique slug; the root scope for all data.
- **Staff User**: A person with login credentials belonging to a single tenant; may have admin or regular staff role.
- **Course**: A named program offered by a tenant with a maximum participant capacity and lifecycle status (draft, active, closed).
- **Session**: A scheduled occurrence of a course at a specific future date and time.
- **Registration**: A participant's enrollment in a specific course session, subject to capacity limits.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can create a tenant and reach the authenticated dashboard in under 2 minutes from first visiting the landing page.
- **SC-002**: An existing staff user can log in and view the course list in under 30 seconds.
- **SC-003**: A staff user can create a new course in under 1 minute from clicking "Add Course" to seeing it in the list.
- **SC-004**: 95% of form validation errors are surfaced inline without full page reloads.
- **SC-005**: The calendar view correctly displays all sessions for the current month without requiring manual refresh.
- **SC-006**: All authenticated actions that modify data provide visible confirmation (success message or immediate UI update) within 3 seconds.
- **SC-007**: Navigating between courses list, calendar, and course detail completes without full page reload.

## Assumptions

- The GUI is a web application running in a modern desktop browser; mobile support is out of scope for this phase.
- The backend API (all modules: Tenants, Identity, Courses, Registrations) is already implemented and running; the GUI communicates exclusively through the existing REST API.
- JWT-based authentication with refresh tokens is the only authentication mechanism; no SSO or OAuth2 is required.
- All data is scoped to a single tenant per login session; users cannot switch tenants without logging out.
- Staff users are created and managed within the app; no external identity provider is involved.
- The calendar view displays sessions (not courses themselves) since sessions carry date/time information.
- Pagination or scroll-based loading is expected for lists that may grow large (courses, registrations, staff users).
- The initial admin account created at tenant setup uses a hardcoded temporary password or one provided during tenant creation — specific password policy is a detail for implementation.
