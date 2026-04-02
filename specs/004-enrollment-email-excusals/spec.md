# Feature Specification: Course Enrollment Email and Session Excusals

**Feature Branch**: `004-enrollment-email-excusals`
**Created**: 2026-04-01
**Status**: Draft
**Input**: When a user is added to a course, they should receive an enrollment confirmation email with a safe link to a public view showing all sessions, with a configurable window to unenroll from the entire course (e.g. 14 days before first session) and to excuse themselves from individual sessions (e.g. 24h before each session); excusals are stored as redeemable credits scoped by course tags and validity windows.

---

## Clarifications

### Session 2026-04-02

- Q: What does "using" an excusal credit mean? → A: The excusal credit grants the participant a makeup enrollment spot in another matching course or session, redeemable self-service.
- Q: Who initiates excusal redemption? → A: The participant redeems it self-service via their safe link page.
- Q: How should course tags for excusal scoping work? → A: Freeform string tags on courses; excusal inherits the source course's tags; redeemable on any course in the same tenant sharing at least one matching tag.
- Q: When "generate excusals" is disabled for a course, what changes? → A: Participants can still excuse themselves from sessions (absence is recorded), but no redeemable excusal credit is issued.
- Q: How is excusal credit validity measured? → A: Tenant admins define a list of named validity windows (e.g., "Q1/2026", "Summer 2026") with date ranges. Each course is assigned one window. An excusal credit is valid for that window plus a configurable number of subsequent windows (e.g., 2 more quarters). The window list is editable by tenant admins.
- Q: When staff soft-deletes an excusal credit, what does the participant see? → A: The credit remains visible on the participant's safe link page shown as "Cancelled by organizer" with no redemption option.
- Q: When staff prolongs an excusal credit, how does that work? → A: Staff selects additional validity windows from the tenant's window list to extend the credit into (consistent with the window-based model).
- Q: Should staff edits to excusal credits be audit-logged? → A: Yes — full audit log with actor, action type, previous value, new value, and timestamp for all staff mutations.
- Q: When staff edits tags on an excusal credit, does it replace or add to existing tags? → A: Replace — the new tag set fully replaces the previous one.
- Q: Can staff restore a soft-deleted excusal credit? → A: No — soft delete is permanent; it exists for auditability only, not for reversal.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enrollment Confirmation Email (Priority: P1)

When staff adds a participant to a course, the participant automatically receives an email confirming their enrollment. The email contains a personal safe link that gives the participant access to a read-only summary of the course without requiring login.

**Why this priority**: This is the primary communication touchpoint between the system and enrolled participants. Without it, participants have no awareness of or access to their enrollment details.

**Independent Test**: Can be fully tested by enrolling a participant in a course and verifying that a confirmation email arrives with a working safe link, without any other feature being implemented.

**Acceptance Scenarios**:

1. **Given** a participant is added to a course by staff, **When** the enrollment is confirmed, **Then** the participant receives an email within a reasonable time containing the course name, a list of upcoming sessions, and a personal safe link.
2. **Given** a participant is enrolled in a course with no upcoming sessions, **When** the enrollment is confirmed, **Then** the participant still receives an email with the course name and a note that no sessions are currently scheduled.
3. **Given** a participant's email address is invalid or missing, **When** enrollment is created, **Then** the system records the enrollment without sending an email and logs the delivery failure for staff review.

---

### User Story 2 - Public Session View via Safe Link (Priority: P2)

A participant clicks the safe link from their enrollment email and sees a personal, read-only page listing all sessions of their course — including dates, times, duration, and location — without needing to log in or create an account.

**Why this priority**: The safe link is the participant's window into the system. It enables all self-service actions (unenrollment, excusals, excusal redemption) and is a prerequisite for stories 3, 4, and 5.

**Independent Test**: Can be fully tested by accessing a generated safe link and verifying session details are displayed accurately and the page is publicly accessible without credentials.

**Acceptance Scenarios**:

1. **Given** a participant has a valid safe link, **When** they open it in a browser, **Then** they see a page with the course name, all scheduled sessions (date, time, duration, location), their enrollment status, and any active excusal credits they hold.
2. **Given** a safe link that has expired or been revoked, **When** a participant tries to open it, **Then** they see a clear message explaining the link is no longer valid and are advised to contact the organizer.
3. **Given** a participant visits the safe link page, **When** the course has already started or ended, **Then** past sessions are displayed but marked as past, and no self-service actions are available for them.

---

### User Story 3 - Unenroll from Entire Course (Priority: P3)

A participant who no longer wishes to attend the course can unenroll themselves via the safe link page, provided the unenrollment deadline (configurable by the tenant) has not yet passed.

**Why this priority**: Gives participants agency over their enrollment and reduces administrative overhead for staff handling manual unenrollment requests.

**Independent Test**: Can be fully tested independently by accessing a safe link within the unenrollment window, clicking unenroll, and verifying the enrollment is removed and staff is notified.

**Acceptance Scenarios**:

1. **Given** a participant visits their safe link within the configured unenrollment window (e.g. 14 days before the first session), **When** they click "Unenroll from course", **Then** their enrollment is cancelled, they receive a confirmation email, and staff is notified.
2. **Given** a participant visits their safe link after the unenrollment window has closed, **When** they view the unenrollment option, **Then** the option is disabled or hidden with a message stating the deadline has passed and they should contact the organizer.
3. **Given** a participant has already unenrolled, **When** they revisit the safe link, **Then** the page reflects their unenrolled status and no further actions are available.

---

### User Story 4 - Excuse from Individual Session (Priority: P4)

A participant can excuse themselves from one or more specific sessions of a course without unenrolling from the entire course, provided the per-session excusal deadline (configurable by the tenant) has not yet passed. When the course has excusal credit generation enabled (globally or per-course override), a redeemable excusal credit is issued to the participant. If generation is disabled, only an absence record is stored.

**Why this priority**: This is a common real-world need (illness, conflict) that reduces "no-show" ambiguity and creates a usable record and/or redeemable credit for administrators and participants.

**Independent Test**: Can be fully tested by submitting an excusal for a specific session within the allowed window and verifying the excusal record is created and (when enabled) an excusal credit is issued.

**Acceptance Scenarios**:

1. **Given** a participant visits their safe link and a session is more than the configured advance notice away (e.g. 24h), **When** they click "Excuse me from this session", **Then** an absence record is created, the participant receives a confirmation, the session is marked as excused on the page, and — if the course has credit generation enabled — a redeemable excusal credit is issued to the participant.
2. **Given** a session is within the excusal deadline (e.g. less than 24h away), **When** the participant views the session, **Then** the excusal option is disabled with a message indicating the deadline has passed.
3. **Given** a participant has already submitted an excusal for a session, **When** they revisit the safe link, **Then** the session shows their excusal status; they cannot submit a duplicate.
4. **Given** staff views the backend, **When** they look at session attendance records, **Then** all stored excusals are visible with participant name, session, timestamp of excusal, and any notes.
5. **Given** the course has excusal credit generation disabled, **When** a participant excuses themselves, **Then** the absence is recorded but no excusal credit is issued; the participant is informed no credit will be generated.

---

### User Story 5 - Excusal Credit Redemption (Priority: P4)

A participant who holds a valid excusal credit can redeem it self-service via their safe link to enroll in a makeup course or session, provided the credit is still within its validity window and the target course shares at least one tag with the source course.

**Why this priority**: The credit only has value if it can be used. Self-service redemption avoids staff bottlenecks for routine makeup requests.

**Independent Test**: Can be fully tested by issuing an excusal credit for a tagged course, navigating to the participant's safe link, selecting a matching makeup course, and verifying enrollment is created.

**Acceptance Scenarios**:

1. **Given** a participant holds a valid excusal credit (within its validity window), **When** they visit their safe link, **Then** they see their available credits with the validity window and the tags they can be redeemed on.
2. **Given** a participant selects a target course for redemption, **When** the target course shares at least one tag with the excusal credit's tags, **Then** the participant is enrolled in the makeup course and the credit is marked as redeemed.
3. **Given** the excusal credit's validity window has expired, **When** the participant views their credits, **Then** the expired credit is shown as inactive and cannot be redeemed.
4. **Given** no matching courses (sharing a tag with the credit) are available in the current or future validity windows, **When** the participant views their credits, **Then** they are informed no matching courses are currently available.

---

### User Story 6 - Staff Excusal Credit Management (Priority: P4)

Staff can view, edit, and soft-delete excusal credits for participants within their tenant. Editable fields are: validity windows (extend by selecting additional windows from the tenant list), tag set (full replacement). The participant field is immutable. All staff mutations are recorded in a full audit log. Soft-deleted credits are permanently deactivated and retained for auditability only.

**Why this priority**: Organizers need the ability to correct mismapped tags, extend credits for exceptional circumstances, or revoke credits issued in error — without being able to reassign credits between participants.

**Independent Test**: Can be fully tested by performing each mutation (prolong, re-tag, soft-delete) via the staff backend and verifying the credit state, participant-facing display, and audit log entry.

**Acceptance Scenarios**:

1. **Given** staff selects an active excusal credit, **When** they extend it by selecting one or more additional validity windows, **Then** the credit's validity range is updated, the change is audit-logged (actor, previous windows, new windows, timestamp), and the participant sees the updated expiry on their safe link page.
2. **Given** staff selects an active excusal credit, **When** they replace its tag set with a new set of tags, **Then** the credit's tags are fully replaced, the change is audit-logged, and future redemption is scoped to the new tags.
3. **Given** staff soft-deletes an excusal credit, **When** the action is confirmed, **Then** the credit status is set to "cancelled", the record is retained in the database, the audit log records the deletion with actor and timestamp, and the participant's safe link page shows the credit as "Cancelled by organizer" with no redemption option.
4. **Given** a soft-deleted excusal credit, **When** any staff member attempts to restore it, **Then** the system rejects the action — soft delete is permanent.
5. **Given** staff attempts to change the participant on an excusal credit, **Then** the system does not expose a participant-editing option; the participant field is read-only in the staff UI.
6. **Given** any staff mutation occurs on an excusal credit, **Then** the audit log entry contains: acting staff member identity, action type, field changed, value before, value after, and timestamp.

---

### User Story 7 - Tenant Settings for Deadlines and Excusal Policy (Priority: P5)

Administrators can configure tenant-wide and per-course settings: unenrollment deadline, excusal deadline, excusal credit generation toggle (global default + per-course override), course tags, and the validity window list.

**Why this priority**: Without configurable policies, the system is too rigid for different course types and business policies.

**Independent Test**: Can be tested by changing each setting and verifying that the corresponding behavior is reflected for participants and staff.

**Acceptance Scenarios**:

1. **Given** an administrator changes the unenrollment deadline setting from 14 days to 7 days, **When** a participant visits a safe link, **Then** the unenrollment option reflects the new 7-day window.
2. **Given** an administrator changes the session excusal deadline from 24h to 48h, **When** a participant views a session 30h in advance, **Then** the excusal option is disabled because the new 48h window applies.
3. **Given** no custom settings have been configured, **When** the system evaluates deadlines, **Then** default values (14 days for unenrollment, 24h for excusals) are applied.
4. **Given** an administrator disables excusal credit generation globally, **When** a participant excuses from a session on a course with no local override, **Then** no credit is issued.
5. **Given** a course has a local override enabling excusal credit generation, **When** a participant excuses from a session on that course, **Then** a credit IS issued regardless of the global setting.
6. **Given** an administrator creates a new validity window "Summer 2026" with a date range and sets the forward-window count to 2, **When** a participant redeems a credit from a course in that window, **Then** the credit is valid through the 2 subsequent windows.

---

### Edge Cases

- What happens when a participant tries to unenroll and they are the last participant in a course — the course is not automatically cancelled, staff must be notified.
- What happens if a course's session schedule changes after enrollment — the safe link page reflects the updated schedule; participants are not automatically re-notified (out of scope for this feature).
- What happens if the same participant is enrolled twice (e.g. duplicate) — the system issues one safe link per enrollment record; duplicate handling is an existing concern.
- What if a participant forwards their safe link to someone else — the safe link is tied to the enrollment and displays only that participant's data; it should not expose other participants' information.
- What happens when a session is cancelled by staff after a participant has already excused themselves — the excusal record is retained as historical data; the credit (if issued) remains valid.
- What if the unenrollment deadline setting is set to 0 — unenrollment is allowed up until the first session starts.
- What if the excusal deadline setting is set to 0 — excusal is allowed until the session starts.
- What if a course has no tags — excusal credits generated from that course are tenant-unrestricted (redeemable on any course in the tenant) OR cannot be generated; this must be a deliberate admin configuration choice.
- What if an excusal credit's validity window list is edited after credits have been issued — existing credits retain the window range they were issued under; changes apply to new credits only.
- What if a participant tries to redeem a credit on a course that is already full — the redemption is rejected with a clear message; the credit remains valid.
- What if staff replaces the tag set on a credit with an empty set — the system must reject this (a credit with no tags would match no courses, rendering it permanently unusable).
- What if staff extends a credit that has already been redeemed or expired — the system must reject extension of non-active credits.
- What if staff soft-deletes a credit that a participant is in the process of redeeming — the delete takes precedence; the redemption fails with an appropriate message to the participant.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST send an enrollment confirmation email to the participant when they are added to a course.
- **FR-002**: The enrollment email MUST contain a unique, personally addressed safe link that grants access to the participant's course view without requiring a login.
- **FR-003**: The safe link MUST remain valid for the duration of the course and a reasonable period afterward (default: until 30 days after the last session).
- **FR-004**: The public course view accessed via safe link MUST display all sessions with their date, time, duration, and location.
- **FR-005**: Users MUST be able to unenroll from the entire course via the safe link, provided the unenrollment deadline has not passed.
- **FR-006**: Users MUST be able to excuse themselves from individual sessions via the safe link, provided the per-session excusal deadline has not passed.
- **FR-007**: System MUST store each excusal as a persistent absence record including: participant identity, session reference, timestamp of excusal creation, and excusal status.
- **FR-008**: Administrators MUST be able to configure the unenrollment deadline as a number of days before the first session (default: 14).
- **FR-009**: Administrators MUST be able to configure the per-session excusal deadline as a number of hours before each session (default: 24).
- **FR-010**: Deadline settings MUST be configurable per tenant (not global).
- **FR-011**: When a participant unenrolls via safe link, the system MUST notify relevant staff (the organizer or admin of the tenant).
- **FR-012**: Past sessions MUST be visible on the safe link page but no self-service actions (excusal, unenrollment) are available for them.
- **FR-013**: Excusal absence records MUST be retrievable by staff for reporting and administrative purposes.
- **FR-014**: The safe link page MUST NOT expose other participants' information.
- **FR-015**: Courses MUST support freeform string tags used to scope excusal credit redemption.
- **FR-016**: System MUST support a tenant-managed list of named validity windows (e.g., "Q1/2026", "Summer 2026"), each with a start date and end date.
- **FR-017**: Each course MUST be assignable to one validity window from the tenant's window list.
- **FR-018**: When excusal credit generation is enabled for a course, excusing from a session MUST issue a redeemable excusal credit to the participant, valid for the course's assigned window plus a configurable number of subsequent windows (default: 2).
- **FR-019**: Excusal credit generation MUST be configurable as a global tenant default, with a per-course override (enabled/disabled).
- **FR-020**: When excusal credit generation is disabled (by global or per-course setting), excusing from a session records absence only — no credit is issued.
- **FR-021**: Participants MUST be able to redeem a valid excusal credit self-service via their safe link to enroll in a makeup course, provided the target course shares at least one tag with the credit's tag set.
- **FR-022**: Upon credit redemption, the participant is enrolled in the target course and the credit is marked as redeemed (single-use).
- **FR-023**: Expired or redeemed excusal credits MUST be visible on the participant's safe link page as inactive (with status and expiry date shown).
- **FR-024**: If a target course is at capacity, credit redemption MUST be rejected and the credit must remain valid.
- **FR-025**: Staff MUST be able to extend an excusal credit's validity by selecting additional validity windows from the tenant's window list; the participant field MUST be immutable to staff.
- **FR-026**: Staff MUST be able to replace the tag set on an excusal credit (full replacement, not additive).
- **FR-027**: Staff MUST be able to soft-delete an excusal credit; soft-deleted credits are permanently deactivated (no restore path) and retained in storage for auditability.
- **FR-028**: A soft-deleted excusal credit MUST appear on the participant's safe link page as "Cancelled by organizer" with no redemption option.
- **FR-029**: All staff mutations to excusal credits (extend, re-tag, soft-delete) MUST be recorded in an immutable audit log containing: acting staff identity, action type, field changed, previous value, new value, and timestamp.

### Key Entities

- **Enrollment**: Links a participant to a course within a tenant. Has a status (active, unenrolled). Carries the safe link token. Represents the source of truth for participation. Lives in the Registrations module.
- **SafeLinkToken**: A unique, unguessable token associated with an enrollment. Used to authenticate the participant's public view without a login. Has an expiry tied to course lifecycle.
- **Excusal**: An absence record that a participant excused themselves from a specific session. Attributes: participant reference, session reference, creation timestamp, status (recorded/credit-issued). Stored persistently. Lives between the Registrations and Courses modules (Registrations module owns it, references Course session by ID).
- **ExcusalCredit**: A redeemable credit issued when a participant excuses from a session on a credit-generating course. Attributes: participant reference (immutable), source session reference, tag set (staff-replaceable), validity window range (staff-extendable; assigned window + N subsequent), status (active/redeemed/expired/cancelled), creation timestamp, deleted_at (soft-delete timestamp, null if active). Lives in the Registrations module.
- **ExcusalCreditAuditEntry**: An immutable log record of a staff mutation on an ExcusalCredit. Attributes: credit reference, acting staff identity, action type (extend/re-tag/soft-delete), field changed, previous value, new value, timestamp. Lives in the Registrations module.
- **ExcusalValidityWindow**: A tenant-managed named time period (e.g., "Q1/2026") with a start date, end date, and sort order. Used to define validity ranges for excusal credits. Lives in the Tenants or Settings module.
- **CourseExcusalPolicy**: Per-course configuration: credit generation enabled/disabled (overrides tenant default), assigned validity window, freeform tags. Stored on or alongside the Course aggregate in the Courses module.
- **TenantExcusalSettings**: Tenant-scoped defaults: excusal credit generation enabled/disabled, forward-window count (default: 2), unenrollment window (days before first session), per-session excusal window (hours before session).
- **EnrollmentEmail**: Represents the outbound notification sent upon enrollment. Tracks delivery status for observability.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of successful course enrollments result in a confirmation email being dispatched to the participant.
- **SC-002**: Participants can access their personal course view via safe link in under 2 seconds.
- **SC-003**: Participants can complete an excusal submission in under 60 seconds from opening the safe link.
- **SC-004**: Participants can complete a course unenrollment in under 60 seconds from opening the safe link.
- **SC-005**: All submitted excusals appear in staff-accessible records within 5 seconds of submission.
- **SC-006**: Safe link pages load correctly on both desktop and mobile browsers.
- **SC-007**: Zero cases of one participant's data being exposed via another participant's safe link.
- **SC-008**: Excusal credit redemption completes (enrollment created, credit marked redeemed) within 5 seconds of participant confirmation.

---

## Assumptions

- Participants are identified by name and email address; they do not have login accounts in the system.
- The email sending infrastructure (SMTP or equivalent delivery service) already exists or will be introduced as part of this feature's technical implementation — the spec does not prescribe the mechanism.
- Tenant administrators have access to a settings screen where deadline values, the validity window list, and excusal policy can be configured; this screen may be new or an extension of an existing settings area.
- Staff notification on unenrollment is via email to the course organizer or a configured admin address; in-app notifications are out of scope for this feature.
- Session schedule changes after enrollment are not re-communicated to participants in this feature; that is a future notification feature.
- Excusal credits carry no automatic capacity-freeing logic (e.g. opening a spot for waitlisted participants) in this version.
- The safe link is single-use in the sense that it is personal and non-transferable, but it is not one-time — it can be revisited multiple times.
- Mobile responsiveness of the public view is expected but a native mobile app is out of scope.
- Excusal credits are single-use — once redeemed, they cannot be re-used even if the makeup enrollment is later cancelled.
- Courses with no tags assigned will require an explicit admin decision on credit generation behaviour (to be resolved during planning).
