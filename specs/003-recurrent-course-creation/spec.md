# Feature Specification: Recurrent Course Creation

**Feature Branch**: `003-recurrent-course-creation`
**Created**: 2026-04-01
**Status**: Draft
**Input**: User description: "In next feature, I would focus on easy working with course creation. E.g. recurent courses, every week, same day, same time should have frontend option so user is not creating everything manually. So I would add option in frontend to create recurent course and user could select session start day, time, every week for 10 weeks and i would in background generate the session array which is sent to backend. Also it user when he sets this, he should be able to view the array before sensing to backend to manually edit it, e.g. delete one item from the session due to holiday etc. he can also then manually adjust the items as necessary. It should also support multiple recurent sessions per week, so the form to create the multi session should be quite compehensive, design all the necessary options and behavior."

## Clarifications

### Session 2026-04-01

- Q: What is the appropriate maximum number of occurrences a single recurrence rule can generate? → A: No hard limit — show a soft warning when occurrences exceed 100, but allow submission regardless.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Define a Single Weekly Recurrence Pattern (Priority: P1)

A staff member wants to create a yoga course that meets every Monday at 09:00 for 12 weeks. Instead of adding 12 sessions manually, they switch to "Recurrence" mode on the course creation form. They pick a day of week, enter a start time, choose a series start date, and enter 12 for the number of occurrences. The system immediately generates and previews a list of 12 sessions below the form.

**Why this priority**: This is the core value proposition of the feature — eliminating repetitive manual data entry for the most common scheduling pattern. Without this, the feature does not exist.

**Independent Test**: Can be fully tested by creating a course using a single weekly recurrence rule and verifying the correct sessions are submitted to the backend.

**Acceptance Scenarios**:

1. **Given** a staff member is on the course creation form, **When** they configure a single weekly rule (day, time, start date, 10 occurrences), **Then** the system generates exactly 10 session entries spaced exactly 7 days apart.
2. **Given** a recurrence rule is configured for Wednesdays with a series start date that is a Thursday, **When** the preview is generated, **Then** the first session falls on the next Wednesday on or after the start date.
3. **Given** the recurrence form is filled in, **When** any rule field is changed (e.g., occurrences changed from 10 to 8), **Then** the session preview updates automatically in real time without a manual refresh action.

---

### User Story 2 - Preview and Edit Sessions Before Submission (Priority: P2)

After generating sessions via a recurrence rule, a staff member reviews the list and notices one session falls on a public holiday. They delete that entry from the list. They also need one session to start 30 minutes later than the rest, so they edit that session's time individually. Then they submit the course.

**Why this priority**: The ability to review and adjust the generated session list is the key differentiator over a blind bulk generator. It gives staff control to handle real-world exceptions (holidays, venue conflicts) without losing the convenience of recurrence.

**Independent Test**: Can be fully tested by generating sessions, removing one, editing another's time, and verifying the final submitted session array reflects exactly the modified list.

**Acceptance Scenarios**:

1. **Given** a generated session list, **When** a staff member deletes a session row, **Then** that session is removed immediately and the remaining sessions retain their correct dates and times.
2. **Given** a generated session list, **When** a staff member edits the date or time of a single session, **Then** only that session changes; all other sessions remain unchanged.
3. **Given** a modified session list (some deleted, some edited), **When** the staff member submits the form, **Then** the backend receives exactly the sessions visible in the final preview — no more, no less.
4. **Given** a staff member has deleted all sessions from the list, **When** they attempt to submit, **Then** the form displays a validation error requiring at least one session.

---

### User Story 3 - Multiple Recurrence Patterns per Week (Priority: P3)

A staff member creates a fitness course meeting Monday at 07:00 AND Thursday at 18:00 for 8 weeks. They add two separate recurrence rules to the form. The system generates 16 sessions total (8 Mondays + 8 Thursdays), merges them in chronological order, and displays the combined preview list.

**Why this priority**: Many courses meet multiple times per week with different times on different days. Without multi-pattern support, staff would still need to manually add sessions for every non-primary day, limiting the feature's usefulness for common scheduling needs.

**Independent Test**: Can be fully tested by adding two recurrence rules with different days and times, verifying the combined preview shows sessions from both rules interleaved in chronological order.

**Acceptance Scenarios**:

1. **Given** a staff member configures two recurrence rules (Mon 07:00, Thu 18:00, 8 weeks each), **When** the preview is shown, **Then** it contains 16 sessions sorted chronologically with correct dates and times for each day.
2. **Given** two active recurrence rules, **When** the staff member removes one rule entirely, **Then** all sessions from that rule are removed from the preview; sessions from the remaining rule are unaffected.
3. **Given** two active rules, **When** a third rule is added, **Then** sessions from all three rules are merged and displayed in chronological order.
4. **Given** two rules where one has an incomplete configuration (e.g., missing start time), **When** the preview attempts to generate, **Then** an inline validation error appears on the incomplete rule and no sessions are generated for it; the fully configured rule's sessions continue to display.

---

### User Story 4 - Manual Session Addition to Supplement Recurrence (Priority: P4)

After generating sessions from a recurrence pattern, a staff member wants to add a one-off make-up session on a specific date that does not fit the pattern. They click "Add session manually" on the preview panel and enter a custom date and time.

**Why this priority**: Courses occasionally need extra sessions outside the regular pattern. This prevents staff from abandoning recurrence mode just to add one custom session.

**Independent Test**: Can be tested by generating recurrent sessions and then adding one manual session, verifying the total count and the custom entry's details in the submitted payload.

**Acceptance Scenarios**:

1. **Given** the session preview panel is visible, **When** the staff member adds a manual session with a specific date and time, **Then** the entry appears in the preview list sorted chronologically among the existing sessions.
2. **Given** a manually added session in the preview, **When** the staff member submits the form, **Then** the manually added session is included in the backend payload alongside all recurrence-generated sessions.

---

### Edge Cases

- What happens when a recurrence rule generates 0 sessions (e.g., start date is after the computed end date, or 0 occurrences entered)?
- What if two recurrence rules produce sessions with the same date and start time (duplicate sessions)?
- What happens when the user switches from "Recurrence" mode to "Manual" mode after sessions have already been generated — are they discarded or converted to manual entries?
- What if the occurrences field is set to the maximum allowed value — does the preview remain performant?
- What happens when a session is edited to a date/time that matches another session in the list (creating a duplicate)?
- What is the behavior when the series start date is left blank and the user tries to generate the preview?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The course creation form MUST offer a "Recurrence" mode alongside the existing manual session entry mode, allowing the user to toggle between them.
- **FR-002**: A recurrence rule MUST capture: day of the week, session start time, series start date, and an end condition (either a number of occurrences or a specific end date).
- **FR-003**: Users MUST be able to add multiple independent recurrence rules to a single course creation form to support courses that meet on different days or at different times within the same week.
- **FR-004**: Users MUST be able to remove any individual recurrence rule; all sessions generated by that rule MUST be removed from the session preview.
- **FR-005**: The session preview list MUST update automatically in real time as recurrence rules are configured, modified, or removed, without requiring any manual refresh or generate action.
- **FR-006**: The session preview list MUST display all generated sessions in chronological order, showing the full date (including day of week), start time, and origin (recurrence-generated vs. manually added) for each entry.
- **FR-007**: Users MUST be able to delete any individual session from the preview list.
- **FR-008**: Users MUST be able to edit the date and/or start time of any individual session in the preview list; editing one session MUST NOT affect any other session.
- **FR-009**: Users MUST be able to add one-off sessions manually to the preview list; manually added sessions MUST be sorted chronologically with the rest.
- **FR-010**: The form MUST display the total count of sessions currently in the preview list, updated in real time.
- **FR-011**: The form MUST prevent submission when the preview list is empty and display a clear validation message.
- **FR-012**: When the form is submitted, the backend MUST receive exactly the session array visible in the final preview, preserving all edits, deletions, and manual additions.
- **FR-013**: There is no hard maximum on the number of occurrences per recurrence rule. When the occurrence count exceeds 100, the form MUST display a soft warning alerting the staff member that they are generating a large number of sessions, but submission MUST still be allowed.
- **FR-014**: When the preview list contains sessions with identical date and start time, the system MUST display a visible warning identifying the duplicates; submission MUST still be allowed.
- **FR-015**: Switching from "Recurrence" mode to "Manual" mode after sessions have been generated MUST prompt the user to confirm before discarding the generated session list.

### Key Entities

- **Recurrence Rule**: A user-configured pattern describing when sessions repeat. Attributes: day of week, start time, series start date, end condition (occurrence count or end date). Exists only in frontend form state — not persisted to the backend independently.
- **Session Preview Entry**: A single session in the editable preview list, either generated from a recurrence rule or added manually. Attributes: date, start time, source type (generated/manual). Represents the transient state before backend submission.
- **Session** (existing): The persisted backend entity for a scheduled course meeting. The submitted array format follows the existing API contract without modification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A staff member can schedule a 10-week single-day weekly course in under 2 minutes, compared to the baseline of manually adding each session.
- **SC-002**: All sessions visible in the final preview are included in the submission payload with 100% fidelity — no sessions silently dropped or added.
- **SC-003**: A course with three different weekly recurrence patterns (e.g., Mon/Wed/Fri at different times) can be configured in a single form interaction without multiple form submissions.
- **SC-004**: The session preview updates within 1 second of any change to a recurrence rule, providing responsive real-time feedback.
- **SC-005**: Staff are never exposed to silent data loss — any action that would discard existing session data requires explicit confirmation from the user.
- **SC-006**: The form remains usable and responsive regardless of occurrence count; a soft warning is displayed when any single rule exceeds 100 occurrences.

## Assumptions

- The existing backend API for course creation accepts an array of session objects with date and time fields; no backend API changes are required for this feature.
- Only weekly recurrence is in scope for v1 (daily, bi-weekly, bi-monthly, and monthly patterns are out of scope).
- All sessions within a single course creation share the same duration; per-session duration override is out of scope for this feature.
- The feature is available only to authenticated staff users with existing course creation permissions; no new permission model is needed.
- The series start date defaults to today's date but is editable.
- The existing manual session entry UI remains fully functional; recurrence mode is an additive option on the same form.
- Timezone handling follows the existing application behavior; no new timezone selection or conversion logic is introduced.
- The recurrence rule configuration panel is part of the existing course creation page, not a separate page or dialog.
