# Feature Specification: Per-Participant Custom Field Values

**Feature Branch**: `006-participant-custom-fields`  
**Created**: 2026-04-03  
**Status**: Draft  
**Input**: User description: "The courses has now fixed columns / metadata, there should be tenant-specific configuration in tenant settings so user can add metadata to course, e.g. paid or paid + deposit - 2 fields, there should be possible to add key/value system - like dictionary to the courses in settings. This should be valid for courses, it would be nice to have it somehow granular for different courses - e.g. more-sessions course will have different values then single session course etc. but I would like not to tied it to the session-count based course. one thing, the fields should be scoped per participant, e.g i create a course, user a sign in and i need to be able to mark if he paid e.g a deposit or complete course"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configure Custom Fields in Tenant Settings (Priority: P1)

As a tenant admin, I want to define custom field definitions (such as "Deposit Paid" or "Full Payment Received") in tenant settings, so that staff have a consistent, configurable set of fields to track per-participant data across courses.

**Why this priority**: Without field definitions there is nothing to assign or record values for — this is the non-negotiable foundation of the entire feature.

**Independent Test**: Navigate to Tenant Settings, create two fields ("Deposit Paid" as Yes/No and "Full Payment" as Yes/No), and verify both appear in the field definitions list. Delivers immediate value by establishing the configuration layer.

**Acceptance Scenarios**:

1. **Given** I am a tenant admin, **When** I open Tenant Settings > Custom Fields and click "Add Field", **Then** I can provide a field name, select a field type (Yes/No, free text, or options list), and save the definition successfully.
2. **Given** I have an existing field definition, **When** I edit its name, **Then** the updated name appears everywhere the field is displayed across all courses.
3. **Given** I attempt to create a field with a name already used by another field in the tenant, **When** I submit, **Then** the system rejects the duplicate and displays an appropriate error message.
4. **Given** I delete a field definition, **When** the deletion is confirmed, **Then** the field no longer appears in any course's participant view.

---

### User Story 2 - Assign Custom Fields to a Course (Priority: P1)

As a staff member creating or editing a course, I want to choose which custom fields are active for that course, so that different courses show only relevant fields and the participant list stays uncluttered.

**Why this priority**: This delivers the "granularity per course" requirement. Without it, all fields appear on all courses regardless of relevance.

**Independent Test**: Create two courses; enable "Deposit Paid" on Course A only. Open each course's participant list and verify Course A shows the "Deposit Paid" column while Course B does not.

**Acceptance Scenarios**:

1. **Given** I am creating or editing a course, **When** I open the course settings, **Then** I see a "Custom Fields" section listing all tenant-level field definitions with a toggle to enable or disable each.
2. **Given** Course A has "Deposit Paid" enabled and Course B does not, **When** I view each course's participant list, **Then** Course A shows the "Deposit Paid" column and Course B does not.
3. **Given** a course has no custom fields enabled, **When** I view its participant list, **Then** the list appears identical to the current experience with no extra columns.
4. **Given** I disable a previously enabled field on a course, **Then** the field column disappears from the participant list but stored values are preserved if the field is re-enabled later.

---

### User Story 3 - Record Field Values Per Enrolled Participant (Priority: P1)

As a staff member managing a course, I want to record and update the value of each active custom field for every enrolled participant directly from the participant list, so that I can track payment status without leaving the course view.

**Why this priority**: This is the core operational use case — the entire feature exists to enable this action.

**Independent Test**: Open a course with "Deposit Paid" enabled and at least one participant enrolled. Toggle the field to "Yes" for one participant, reload the page, and verify the value persisted.

**Acceptance Scenarios**:

1. **Given** a course has one or more custom fields enabled, **When** I view the participant list, **Then** each enabled field appears as a column with the current value shown per participant row.
2. **Given** I see a participant row with a Yes/No field, **When** I toggle the field, **Then** the value saves immediately without requiring a separate save button.
3. **Given** a participant is enrolled in Course A and Course B, **When** I mark "Deposit Paid = Yes" for them in Course A, **Then** Course B still shows an independent value for that participant (values do not carry over between courses).
4. **Given** a text or options-list field is enabled, **When** I click the field cell for a participant, **Then** I can enter or select a value inline and the change saves on blur or selection.

---

### User Story 4 - View Summary Counts for Field Values (Priority: P3)

As a staff member, I want to see at a glance how many participants have a given field value set (e.g., "3 of 10 paid deposit"), so that I can assess overall payment status without inspecting each row individually.

**Why this priority**: Convenience improvement. The core tracking works without aggregated counts.

**Independent Test**: Enroll 10 participants in a course, mark 3 as "Deposit Paid = Yes", and verify the participant list column header or footer shows "3 / 10".

**Acceptance Scenarios**:

1. **Given** a course has 10 participants and 3 have "Deposit Paid = Yes", **When** I view the participant list, **Then** the column header or footer for "Deposit Paid" shows "3 / 10".
2. **Given** the summary count is visible, **When** I update a participant's value, **Then** the count updates immediately to reflect the change.

---

### Edge Cases

- What happens when a participant is unenrolled from a course? — Their field values for that enrollment are permanently deleted.
- What happens when a field definition is deleted from tenant settings? — The field disappears from all course participant lists; values previously stored are silently orphaned and no longer accessible.
- What happens when an options-list field has a choice removed after values have been saved? — Existing values referencing the removed choice are preserved but displayed with a visual indicator ("no longer a valid option").
- What happens when a field definition name is changed? — The new name appears everywhere immediately; no stored values are affected.
- What happens when a course has no custom fields assigned? — The participant list shows only fixed columns with no change to the current experience.
- Can two participants in the same course have independent values for the same field? — Yes, values are always per-enrollment, never shared.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Tenant admins MUST be able to create custom field definitions in tenant settings, specifying a name and type for each field.
- **FR-002**: Supported field types MUST include: Yes/No (boolean toggle), free text, and options list (predefined selectable values).
- **FR-003**: Field definitions MUST be tenant-scoped; tenants cannot see or use each other's definitions.
- **FR-004**: Field names MUST be unique within a tenant; the system MUST reject duplicate names with an error message.
- **FR-005**: Tenant admins MUST be able to edit the name and allowed options of an existing field definition.
- **FR-006**: Tenant admins MUST be able to delete a field definition; upon deletion, the field MUST be removed from all course participant views.
- **FR-007**: Staff MUST be able to enable or disable specific custom fields per course from within course settings; all tenant field definitions are available for selection.
- **FR-008**: Staff MUST be able to view and edit custom field values for each enrolled participant inline in the participant list, without navigating away.
- **FR-009**: Field value changes MUST be auto-saved on interaction (toggle, blur, or selection) with no explicit save button required.
- **FR-010**: Custom field values MUST be stored per enrollment (per participant per course); the same person enrolled in two courses has completely independent values.
- **FR-011**: When a participant is unenrolled from a course, all their field values for that enrollment MUST be deleted.
- **FR-012**: When a course has no custom fields enabled, the participant list MUST display identically to the current experience with no additional columns.

### Key Entities

- **CustomFieldDefinition**: A tenant-level template for a metadata field. Has a name, type (Yes/No / text / options list), and an optional ordered list of allowed values (for options-list type). Belongs to exactly one tenant.
- **CourseFieldAssignment**: Links a CustomFieldDefinition to a specific course, indicating the field is active for that course. Controls which columns appear in that course's participant list.
- **ParticipantFieldValue**: The actual recorded value of a CustomFieldDefinition for a specific enrolled participant. Tied to a registration/enrollment, not to the participant globally.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A tenant admin can create a new custom field definition and have it available for assignment to a course in under 60 seconds.
- **SC-002**: Staff can record or update a participant's field value in 2 interactions or fewer (e.g., one click to toggle a Yes/No field).
- **SC-003**: Field values are fully isolated per enrollment — marking a value for Participant A in Course X has zero effect on their value in Course Y (100% verified across all field types).
- **SC-004**: Courses with no custom fields assigned display a participant list indistinguishable from the current baseline with no regression in the existing experience.
- **SC-005**: Staff can determine which participants have not yet completed payment by scanning the participant list without opening any additional screens.

## Assumptions

- Tenant admin and staff may be the same role or staff has sufficient access; tenant admin has access to global tenant settings while course field assignments are configured per-course by staff.
- The primary editing surface for participant field values is the course participant list — no separate data-entry screen is required.
- Options-list values are defined at field-definition time; the list of allowed values can be extended or reduced later by the tenant admin.
- Bulk import or export of field values (e.g., via CSV) is out of scope for this version.
- No notifications or automated workflows are triggered by field value changes (e.g., no email when a participant is marked as "paid").
- Field values are informational and operational only — they do not affect enrollment eligibility, course capacity, or any other system behavior.
- This feature applies to the staff-facing interface only; participants do not see custom field values in their own course view.
- This feature depends on the existing enrollment/registration system remaining structurally unchanged.
