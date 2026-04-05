# Research: Course Data Export

**Phase**: 0 — Research  
**Branch**: `008-course-data-export`  
**Date**: 2026-04-04

---

## R-001: CSV Generation Library

**Decision**: Use `CsvHelper` (NuGet: `CsvHelper`) in the backend for RFC 4180-compliant CSV generation.

**Rationale**: CsvHelper handles all edge cases (quoting, escaping, newlines in values) with zero boilerplate. It writes UTF-8 with BOM by default, which is required for correct Excel rendering. No existing file-generation code exists in the codebase — this must be introduced from scratch.

**Alternatives considered**:
- Manual string building — fragile, error-prone for edge cases (commas, quotes, newlines inside values). Rejected.
- EPPlus / ClosedXML (XLSX) — out of scope for this iteration per spec.

---

## R-002: Cross-Module Data Assembly

**Decision**: API-layer orchestration. The export endpoint in `Terminar.Api` calls two separate MediatR queries — one from the Courses module and one from the Registrations module — and passes the combined data to a `CsvExportService` for serialization.

**Rationale**: Courses and Registrations live in separate DbContexts and cannot be joined at the database level. The existing pattern for cross-module data (e.g., `GetCourseCustomFieldsQuery` called from `GetCourseRosterQueryHandler`) uses MediatR for cross-cutting reads. For export, the volume of data and the need to merge two result sets makes API-layer assembly the simplest correct approach. The API layer already has access to `IMediator` and `ITenantContext`.

**Alternatives considered**:
- Export handler inside Registrations module calling Courses module via MediatR — this would embed orchestration inside a domain-module handler, conflating concerns. Rejected.
- Dedicated Export module — adds structural overhead for one feature. Rejected.
- Shared DbContext — violates constitution Principle IV (clean architecture / no shared infrastructure). Rejected.

---

## R-003: New Query Handlers Required

**Decision**: Add two new application-layer queries:

1. `ExportCoursesQuery` (Courses module) — returns all courses for a tenant matching optional filters (date range, status), with no pagination. Returns a flat DTO list.
2. `ExportCourseRosterQuery` (Registrations module) — given a list of course IDs and a tenant ID, returns all registrations with field values for those courses in one DB round-trip. Returns a flat DTO list with custom field columns.

**Rationale**: Existing queries (`ListCoursesHandler`, `GetCourseRosterQueryHandler`) are paginated and not suitable for bulk export. New purpose-built handlers avoid modifying well-tested existing handlers and keep export concerns isolated.

---

## R-004: File Response Pattern

**Decision**: Use `Results.File(byte[], "text/csv; charset=utf-8", filename)` from ASP.NET Core Minimal APIs.

**Rationale**: `Results.File` is the idiomatic way to return file downloads in Minimal API endpoints. Content-Disposition is set to `attachment` automatically, triggering a browser download.

**Alternatives considered**:
- `Results.Stream` — useful for large streaming responses. Deferred; 10 000-row limit means byte array is fine.
- Custom IResult — unnecessary complexity. Rejected.

---

## R-005: Frontend Download Trigger

**Decision**: Add a `downloadFile(url: string, filename: string)` utility to `frontend/src/lib/download.ts`. This utility calls `fetch()` with auth headers (from existing auth store), receives a `Blob`, and triggers a browser download via a temporary anchor element.

**Rationale**: The existing `apiFetch` utility always parses JSON and is not suitable for binary responses. A minimal dedicated download helper avoids modifying the shared API client and reuses the existing auth token handling.

---

## R-006: Export Entry Points in Frontend

**Decision**:
- **Courses list page** (`CourseListPage.tsx`): Add "Export" button in the page action area. On click, opens `ExportCoursesModal` — a drawer/modal with all export options (filters, include-participants toggle, column selection).
- **Course roster page** (`CourseRosterPage.tsx`): Add "Export CSV" button in the roster action area. On click, opens a simplified `ExportRosterModal` with only column selection (course context is implicit).

**Rationale**: Matching the two entry points defined in the spec. The options for the roster export are simpler (no course filter needed, no include-participants toggle) so a lighter modal is appropriate.

---

## R-007: Column Selection State Management

**Decision**: Export modal component holds column selection state in local React state, pre-populated from `sessionStorage`. After each export, the selection is saved back to `sessionStorage` under a stable key (e.g., `export_columns_courses`, `export_columns_roster`).

**Rationale**: The spec requires session-scoped persistence of last-used settings. `sessionStorage` is cleared on tab close, matching "same browser session" semantics. No backend persistence is needed.

---

## R-008: No New Database Migrations

**Decision**: This feature introduces no new database tables, columns, or EF Core migrations.

**Rationale**: Export is a pure read operation over existing data. All required data (`Course`, `Session`, `Registration`, `ParticipantFieldValue`, `CustomFieldDefinition`) already exists in the database.

---

## R-009: Column Definitions

**Decision**: Column sets are defined statically in the backend export service as named groups, and surfaced to the frontend via a dedicated endpoint `GET /api/v1/courses/export/columns`.

**Rationale**: Hardcoding column definitions in both frontend and backend creates a synchronization risk. By serving the available columns from the backend, the frontend always reflects what the export actually produces. Each column definition includes: `key`, `label` (i18n key), `group` (courses | participants | customFields), `defaultEnabled` (bool).

**Alternatives considered**:
- Static column list in frontend only — simpler but risks drift. Rejected.
- Dynamic column discovery at export time — unnecessary complexity. Rejected.

---

## R-010: Filtering Alignment with Existing Frontend Filters

**Decision**: The export options modal re-uses the same filter vocabulary as the existing `useCoursesFilter()` hook (date buckets translated to absolute dates, status enum values). Filters are passed as query parameters to the export endpoint.

**Rationale**: Consistency with the existing filter model reduces cognitive load. The user's current filter state from the courses list could optionally be pre-populated into the export modal as a future enhancement.

---

## R-011: i18n for New UI Strings

**Decision**: All new UI strings (modal titles, labels, button text, column names) are added to the existing i18n JSON files for Czech (`cs`) and English (`en`) as required by the constitution's Multi-Language First principle.

**Rationale**: Constitution Principle III — no hardcoded user-facing strings.

---

## R-012: Excusal Count Column

**Decision**: When building the participant export, the `ExportCourseRosterQueryHandler` sends a cross-module query `GetExcusalCountsForCourseQuery` (if the Excusals plugin is active for the tenant) and appends an `excusal_count` column to the result. Plugin activation is checked via `IPluginActivationService`.

**Rationale**: Spec FR-009 requires this column to be present when the Excusals feature is active and absent otherwise. The existing plugin activation check pattern (from feature 007) is reused.

**Alternatives considered**:
- Always include excusal count (set to 0 when plugin inactive) — misleading for tenants without excusals. Rejected.
- Frontend-side toggle — the backend should own the column list to avoid leaking plugin state to the frontend. Rejected.
