# Feature Specification: Course Reservation System (Termínář)

**Feature Branch**: `001-course-reservation-system`
**Created**: 2026-03-30
**Status**: Draft
**Input**: User description: "Project name is Termínář, it is a reservation system for managing courses. It will be multi-purpose system, multi-language system. We will start the implementation tailored for one use case. Later we will generalize it more. The system should allow by staff of the current instance to write courses, allow people to register to it or only staff can add them. It should be one-time course or multi-instance course, so they e.g. 11 courses."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Staff Creates a Course (Priority: P1)

A staff member creates a new course in the system. They provide the course name, description, date(s), capacity, and choose whether it is a one-time event or a multi-session course (e.g., 11 weekly sessions). They also set the registration mode — whether participants can self-register or must be added by staff.

**Why this priority**: Without courses, there is nothing to register for. This is the foundational capability of the system.

**Independent Test**: A staff member can log in, create a one-time course with a title, date, and capacity limit, and see it listed in the course catalogue. This alone demonstrates the core data entry workflow.

**Acceptance Scenarios**:

1. **Given** a logged-in staff member, **When** they create a one-time course with title, description, date, time, and capacity, **Then** the course appears in the public course list with correct details.
2. **Given** a logged-in staff member, **When** they create a multi-session course specifying the number of sessions and each session's date/time, **Then** all sessions are created and visible as part of the same course.
3. **Given** a logged-in staff member, **When** they set a course to "staff-only registration", **Then** the self-registration option is not available to regular users for that course.
4. **Given** a logged-in staff member, **When** they set a course to "open registration", **Then** participants can register themselves for that course.

---

### User Story 2 - Participant Self-Registers for a Course (Priority: P2)

A participant browses available courses and registers themselves for one that has open registration enabled.

**Why this priority**: Self-registration reduces staff workload and scales the system beyond manual enrollment.

**Independent Test**: A participant can view the course list, select an open-registration course, submit their registration, and receive confirmation — without any staff action required.

**Acceptance Scenarios**:

1. **Given** an open-registration course with available capacity, **When** a participant submits a registration with their details, **Then** they are registered and receive a confirmation.
2. **Given** a course that has reached its maximum capacity, **When** a participant attempts to register, **Then** they are informed the course is full and registration is not accepted.
3. **Given** a course with staff-only registration, **When** a participant tries to self-register, **Then** the system does not allow registration and informs the participant.
4. **Given** a participant who is already registered, **When** they attempt to register again for the same course, **Then** the system prevents duplicate registration and shows an appropriate message.

---

### User Story 3 - Staff Registers a Participant Manually (Priority: P2)

A staff member adds a participant to a course on their behalf — either for staff-only courses or as an override for any course.

**Why this priority**: Manual registration by staff is needed for courses that are not self-service and for handling edge cases (e.g., registering someone who called in by phone).

**Independent Test**: A staff member can open a course, enter participant details, and add them to the course roster — the participant appears in the attendance list.

**Acceptance Scenarios**:

1. **Given** a staff-only course, **When** a staff member adds a participant with their name and contact details, **Then** the participant appears on the course roster.
2. **Given** a course at full capacity, **When** a staff member tries to add another participant, **Then** the system warns about the capacity limit (and may optionally allow override).
3. **Given** a participant already on the roster, **When** a staff member tries to add them again, **Then** the system prevents the duplicate and shows an appropriate message.

---

### User Story 4 - Participant Cancels Registration (Priority: P3)

A registered participant cancels their reservation for a course they can no longer attend.

**Why this priority**: Cancellation frees up spots for others and keeps attendance data accurate.

**Independent Test**: A participant can access their registration, cancel it, receive a confirmation, and the spot becomes available to others.

**Acceptance Scenarios**:

1. **Given** a registered participant, **When** they cancel their registration before the course date, **Then** the registration is removed and the spot is freed.
2. **Given** a registered participant, **When** they attempt to cancel after the course has already taken place, **Then** the system informs them that cancellation is no longer possible.

---

### User Story 5 - Staff Views Course Roster (Priority: P2)

A staff member views the list of registered participants for a course to prepare for delivery.

**Why this priority**: Staff need to know who is attending to manage logistics and confirm attendance.

**Independent Test**: A staff member can navigate to any course they manage and see a full list of registered participants with their contact details.

**Acceptance Scenarios**:

1. **Given** a course with registered participants, **When** a staff member opens the course roster, **Then** they see all participants with name and contact information.
2. **Given** a multi-session course, **When** a staff member views the roster, **Then** the system shows participants registered for the full course.

---

### Edge Cases

- What happens when a course is cancelled after participants have already registered — are they notified?
- What happens when a staff member deletes a course that already has registrations?
- How does the system handle a multi-session course where a participant registers after some sessions have already passed?
- What if two participants simultaneously attempt to claim the last available spot in a course?
- What happens if a participant uses an email address that is already registered for the same course?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow staff to create courses with a title, description, date/time, capacity limit, and registration mode (open or staff-only).
- **FR-002**: System MUST support two course types: one-time (single session) and multi-session (a defined number of sessions, each with its own date and time).
- **FR-003**: System MUST allow participants to self-register for courses with open registration, providing their name and contact details.
- **FR-004**: System MUST allow staff to manually add participants to any course regardless of registration mode.
- **FR-005**: System MUST enforce capacity limits — registration MUST be blocked when a course is full, with a clear message shown to the user.
- **FR-006**: System MUST prevent duplicate registrations — the same participant (identified by email) MUST NOT be registered twice for the same course.
- **FR-007**: System MUST allow registered participants to cancel their own registration before the course date.
- **FR-008**: System MUST allow staff to view the full participant roster for any course they manage.
- **FR-009**: System MUST send a confirmation notification to participants upon successful registration.
- **FR-010**: System MUST allow staff to remove individual participant registrations from a course.
- **FR-011**: System MUST support multi-language display — course details and the system interface must be displayable in more than one language.
- **FR-012**: System MUST allow staff to edit course details (title, description, dates, capacity) after creation, as long as the course has not yet taken place.
- **FR-013**: System MUST support multiple independent instances (tenants) from day one, each with fully isolated staff, courses, and participants — no data from one instance is visible or accessible from another.

### Key Entities

- **Course**: Represents a learning or training event. Has a title, description, type (one-time or multi-session), registration mode (open or staff-only), maximum capacity, and status (draft, active, cancelled, completed).
- **Session**: A single occurrence within a course. Has a scheduled date, start time, duration, and optional location. A one-time course has exactly one session; a multi-session course has many.
- **Registration**: Links a participant to a course. Records when the registration was made, who created it (self or staff), and its current status (confirmed, cancelled).
- **Participant**: A person registered for a course. Has a name, email address, and optional additional contact information. No login account required.
- **Staff User**: An authenticated user with permissions to create and manage courses and registrations within their instance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff can create a fully configured course (including all sessions for multi-session courses) in under 5 minutes.
- **SC-002**: Participants can find and complete self-registration for an open course in under 2 minutes.
- **SC-003**: 95% of registration attempts succeed on first try without errors or confusion.
- **SC-004**: Staff can view the full participant roster for any course in under 30 seconds.
- **SC-005**: The system correctly enforces capacity limits — zero cases of a course accepting more registrations than its defined cap.
- **SC-006**: All user-facing content is available in at least two languages with no untranslated strings visible to end users.

## Assumptions

- Multi-tenancy is a first-class requirement: each instance (tenant) has its own isolated staff, courses, and participants from the start. The initial deployment will run one tenant, but the system must support adding more without structural changes.
- Participants do not need to create user accounts — registration is guest-based (name + email).
- Staff users authenticate with a username and password; single sign-on is out of scope for v1.
- Email is the primary notification channel for registration confirmations and cancellations.
- A mobile-responsive web interface is the delivery format; native mobile apps are out of scope for v1.
- For multi-session courses, participants register for the entire course (all sessions), not individual sessions — per-session registration is a future enhancement.
- The system does not handle payments or fees in v1.
- Course content delivery (materials, recordings, etc.) is out of scope — the system handles scheduling and registration only.
