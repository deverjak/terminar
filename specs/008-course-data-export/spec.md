# Feature Specification: Course Data Export

**Feature Branch**: `008-course-data-export`  
**Created**: 2026-04-04  
**Status**: Draft  
**Input**: User description: "For courses and its participants, I would like to add option to export data option to .csv or any other tabular format. For course, user should also be able to export participants with option yes/no or just courses without participants. There should be some options. Design real-world specification and design export for real world usage. It is not a plugin, but an integral part of the application."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export Course List Without Participants (Priority: P1)

A staff member wants to get an overview of all courses in a spreadsheet — for reporting, auditing, or sharing with management. They initiate an export from the courses list view, select "courses only" (no participant data), optionally filter by date range or status, and download a CSV file that opens correctly in Excel or Google Sheets.

**Why this priority**: This is the simplest, highest-value use case. A course-only export has no privacy implications and serves the most common reporting needs. It establishes the foundational export pipeline for all other export types.

**Independent Test**: Triggering export with "include participants: No" produces a valid CSV containing one row per course with all relevant course-level columns.

**Acceptance Scenarios**:

1. **Given** a staff member is on the courses list page, **When** they click "Export" and choose to export without participants, **Then** a CSV file is downloaded containing one row per course with columns: name, description, start date, end date, location, capacity, enrolled count, status.
2. **Given** a staff member applies a date range filter (e.g., April–June 2026) before exporting, **When** they download the file, **Then** only courses starting within that range are included.
3. **Given** a staff member applies a status filter (e.g., "Active only") before exporting, **When** they download the file, **Then** only courses matching that status appear.
4. **Given** the export contains multi-language or special characters (e.g., Czech names with diacritics), **When** the file is opened in Excel, **Then** all characters display correctly without encoding issues.
5. **Given** there are no courses matching the selected filters, **When** the export is triggered, **Then** a CSV with only the header row is downloaded and the user sees a warning that no data was found.

---

### User Story 2 - Export Course List With Participants Included (Priority: P2)

A staff member needs a full enrollment report — one flat file containing each enrollment as a separate row, with course information repeated on each row. This is used for mail merge, invoicing follow-up, or regulatory compliance reporting.

**Why this priority**: This is the most data-rich export variant and the one users rely on most heavily for operational tasks. It builds directly on Story 1's export pipeline.

**Independent Test**: Triggering export with "include participants: Yes" produces a valid CSV where each row represents one participant in one course, with both course columns and participant columns present.

**Acceptance Scenarios**:

1. **Given** a staff member exports with "include participants: Yes", **When** the file is downloaded, **Then** each row contains course fields (name, date, location) plus participant fields (first name, last name, email, phone, enrollment status, enrollment date).
2. **Given** a course has zero enrolled participants, **When** it is included in a with-participants export, **Then** it appears as a single row with participant fields left blank — courses with no enrollments remain visible rather than being silently omitted.
3. **Given** participants have custom field values (e.g., "Company", "Dietary restrictions"), **When** exported with participants, **Then** each defined custom field appears as its own column; cells are empty where a participant has no value for that field.
4. **Given** a course has 200 participants and multiple courses are selected, **When** the export is triggered, **Then** the file contains all participant rows and downloads within 30 seconds.
5. **Given** a participant's name or notes contain commas, quotes, or newlines, **When** exported, **Then** those fields are properly quoted per RFC 4180 so the file parses correctly in any spreadsheet application.

---

### User Story 3 - Export a Single Course's Participant Roster (Priority: P2)

A course coordinator opens a specific course's detail page and wants to export just that course's participant list — names, contact details, enrollment status — for printing, offline use, or sharing with a venue.

**Why this priority**: Contextual single-course export is a common day-to-day workflow for coordinators and is accessed from a different entry point than bulk list export.

**Independent Test**: Triggering export from within a single course detail page produces a CSV containing only that course's participants with all relevant participant columns.

**Acceptance Scenarios**:

1. **Given** a staff member is on the course detail page, **When** they click "Export participants", **Then** a CSV is downloaded containing one row per participant for that course, with columns: first name, last name, email, phone, enrollment status, enrollment date, plus any custom fields defined for that tenant.
2. **Given** the course has participants and the Excusals feature is active for the tenant, **When** exported, **Then** an "Excusal count" column shows how many excusals each participant has submitted.
3. **Given** the course has no participants, **When** export is triggered, **Then** a CSV with only the header row is downloaded.

---

### User Story 4 - Select Columns and Configure Export Options (Priority: P3)

A staff member wants to control exactly which columns appear in the export — to avoid sharing sensitive data, to reduce noise, or to match a specific reporting template. Before downloading, they see an options panel where they can toggle individual columns on or off.

**Why this priority**: Column selection improves real-world utility significantly but is not required for a working MVP.

**Independent Test**: Deselecting specific columns in the export options panel results in those columns being absent from the downloaded file.

**Acceptance Scenarios**:

1. **Given** the export options panel is open, **When** a staff member deselects "Phone" and "Email", **Then** the downloaded file contains no phone or email columns.
2. **Given** a staff member has previously configured an export (columns, filters), **When** they initiate a new export in the same browser session, **Then** their last-used settings are pre-selected as defaults.
3. **Given** a staff member deselects all columns, **When** they attempt to download, **Then** the system prevents the export and displays a validation message requiring at least one column to be selected.

---

### Edge Cases

- What happens when the total export size is very large (e.g., 10 000+ participant rows)? The export must still complete; if processing time exceeds a threshold, a progress indicator is shown. Silent timeouts or partial exports are not acceptable.
- How does the system handle concurrent exports by multiple staff members? Each export is independent and isolated — no locking or shared state between users.
- What if a course name or field value contains the CSV delimiter character? All string fields are RFC 4180 quoted regardless of content.
- What happens when date filters result in an empty dataset? A file with only the header row is returned and the user is shown a descriptive message.
- What if custom field definitions are changed after an export is in progress? The export uses the field definitions as they existed at the moment the export was initiated.
- What if a participant's data is incomplete (missing email or phone)? The cell is left empty; the row is still included.
- What if a course has both enrolled and waitlisted participants — which are included? Both enrollment statuses are included; the "enrollment status" column distinguishes them.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an "Export" action on the courses list view accessible to all authenticated staff members.
- **FR-002**: System MUST present an export options panel before generating the file, offering at minimum: include-participants toggle, date range filter, course status filter, and column selection.
- **FR-003**: System MUST allow filtering the export by course start date range (from / to dates, both optional).
- **FR-004**: System MUST allow filtering the export by course status (all / active / past / cancelled).
- **FR-005**: System MUST generate a CSV file conforming to RFC 4180 with UTF-8 encoding and a UTF-8 BOM for correct Excel rendering.
- **FR-006**: System MUST include the following columns in a courses-only export: course name, description, start date, end date, location, total capacity, enrolled count, waitlisted count, status.
- **FR-007**: System MUST include all course-level columns plus participant fields in a with-participants export: participant first name, last name, email, phone, enrollment status, enrollment date.
- **FR-008**: System MUST include all active tenant custom participant fields as additional columns in any participant-inclusive export; cells for participants with no value for a given field MUST be empty rather than omitted.
- **FR-009**: System MUST include an "Excusal count" column per participant when the Excusals feature is active for the tenant; the column MUST be absent when the feature is inactive.
- **FR-010**: System MUST provide an "Export participants" action on the individual course detail page, producing a participant roster for that single course.
- **FR-011**: System MUST allow staff to toggle individual columns on or off in the export options panel before generating the file.
- **FR-012**: System MUST retain the user's last-used export settings (column selection, filters) within the current browser session and pre-apply them as defaults for subsequent exports.
- **FR-013**: System MUST prevent export submission when no columns are selected and display a clear validation message.
- **FR-014**: System MUST produce a flat/denormalized CSV (one row per participant per course) when exporting with participants — course columns are repeated on every row.
- **FR-015**: System MUST name downloaded files descriptively and include the export date (e.g., `courses-export-2026-04-04.csv`, `course-participants-2026-04-04.csv`).
- **FR-016**: System MUST show a user-friendly message when applied filters produce an empty result set.
- **FR-017**: System MUST handle exports of at least 10 000 rows without data loss, corruption, or silent truncation.

### Key Entities

- **Course Export Row**: Represents one course in a courses-only export. Contains course-level attributes: name, dates, location, capacity metrics, status.
- **Participant Export Row**: Represents one enrollment in a with-participants export. Combines course-level attributes with participant-level attributes in a flat/denormalized structure.
- **Export Options**: A transient configuration capturing the user's selected columns, date range filter, status filter, and include-participants choice. Not persisted to the database — lives in browser session state only.
- **Custom Field Column**: A dynamic column derived from tenant-defined participant custom fields. Present in all participant-inclusive exports; absent in courses-only exports.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Staff members can initiate and download a course export in under 5 user interactions (clicks/selections) from the courses list page.
- **SC-002**: Exports containing up to 10 000 rows complete and the file is available for download within 30 seconds.
- **SC-003**: Downloaded CSV files open correctly in Microsoft Excel and Google Sheets without manual encoding adjustments — special characters and diacritics render as expected.
- **SC-004**: 100% of data rows matching the selected filters are present in the export — no rows are silently truncated or omitted.
- **SC-005**: Column selection covers all data fields visible in the application — no data visible in the UI is unexportable.
- **SC-006**: The exported CSV is parseable by standard CSV libraries without post-processing to fix quoting or delimiter issues (RFC 4180 compliance).

## Assumptions

- Only authenticated staff members (not public users or participants themselves) have access to export functionality.
- The export format for this iteration is CSV (UTF-8 with BOM). XLSX (Excel native format with multiple sheets) is out of scope and deferred to a future enhancement.
- Exports are generated synchronously for datasets up to 10 000 rows. Asynchronous background export for larger datasets is out of scope for this iteration.
- Column selection state is session-scoped (not persisted to a user account profile) — sufficient for most use cases without added complexity.
- Custom participant fields already defined by the tenant (feature 006) are available for inclusion in exports; this feature introduces no new custom field capabilities.
- Excusal data is included as a column only when the Excusals feature is active for the tenant; the column is simply absent otherwise. No configuration by the user is needed.
- Date values in the CSV use ISO 8601 format (YYYY-MM-DD) to ensure consistent parsing across all locale settings.
- The export action is surface-level accessible (visible button in courses list and course detail pages) — no separate settings page is required to enable it.
- Multi-sheet XLSX export (summary + participants in separate sheets) is explicitly out of scope for this iteration.
- Both enrolled and waitlisted participants are included in exports; the enrollment status column distinguishes between them.
