# Quickstart: Course Data Export

**Feature**: `008-course-data-export`  
**Date**: 2026-04-04

---

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker (for PostgreSQL via Aspire)
- An existing tenant with at least 2–3 courses and a few registrations

---

## Running the Application

```bash
# Start backend (PostgreSQL + API via Aspire)
cd src/Terminar.AppHost && dotnet run

# Start frontend (in separate terminal)
cd frontend && npm install && npm run dev
```

---

## Testing the Export Feature

### 1. Get a Staff JWT

```bash
# From repo root
dotnet user-jwts create --role Staff
```

Copy the token value for use in curl commands below.

### 2. Check Available Columns

```bash
curl -H "Authorization: Bearer <TOKEN>" \
     -H "X-Tenant-Slug: my-tenant" \
     "http://localhost:5000/api/v1/courses/export/columns"
```

Expected: JSON with column definitions including course info columns and any custom fields.

### 3. Export Courses Only (CSV)

```bash
curl -H "Authorization: Bearer <TOKEN>" \
     -H "X-Tenant-Slug: my-tenant" \
     "http://localhost:5000/api/v1/courses/export?include_participants=false&columns=course_title&columns=course_status&columns=course_first_session_at&columns=course_enrolled_count" \
     --output courses-export.csv
```

Open `courses-export.csv` in Excel. Verify: one row per course, correct columns, diacritics render correctly.

### 4. Export With Participants

```bash
curl -H "Authorization: Bearer <TOKEN>" \
     -H "X-Tenant-Slug: my-tenant" \
     "http://localhost:5000/api/v1/courses/export?include_participants=true&columns=course_title&columns=participant_name&columns=participant_email&columns=enrollment_status" \
     --output courses-with-participants.csv
```

Verify: one row per participant, course columns repeated, participant fields present.

### 5. Export Single Course Roster

```bash
# Replace COURSE_ID with a real UUID
curl -H "Authorization: Bearer <TOKEN>" \
     -H "X-Tenant-Slug: my-tenant" \
     "http://localhost:5000/api/v1/courses/COURSE_ID/registrations/export?columns=participant_name&columns=participant_email&columns=enrollment_date" \
     --output roster.csv
```

### 6. Test Empty Result Handling

```bash
# Use a date range with no courses
curl -H "Authorization: Bearer <TOKEN>" \
     -H "X-Tenant-Slug: my-tenant" \
     "http://localhost:5000/api/v1/courses/export?date_from=2099-01-01&date_to=2099-12-31&columns=course_title" \
     --output empty.csv
```

Expected: CSV with only the header row (no data rows).

### 7. Test Validation Errors

```bash
# Missing columns parameter — should return 400
curl -v -H "Authorization: Bearer <TOKEN>" \
     -H "X-Tenant-Slug: my-tenant" \
     "http://localhost:5000/api/v1/courses/export"
```

---

## Frontend Walkthrough

1. Navigate to **Courses** list page
2. Click the **Export** button in the page toolbar
3. In the export modal:
   - Toggle "Include participants" on/off
   - Set a date range
   - Check/uncheck columns
4. Click **Download CSV** — file should download immediately
5. Navigate to any course → **Participants** tab → click **Export CSV**
6. Verify column selection is remembered within the same browser session

---

## RFC 4180 Compliance Spot-Check

To verify the CSV is well-formed, run:

```python
import csv
with open("courses-with-participants.csv", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    for row in reader:
        print(row)
```

A clean parse with no errors indicates RFC 4180 compliance.

---

## Key Files After Implementation

| File | Purpose |
|------|---------|
| `src/Terminar.Modules.Courses/Application/Queries/ExportCourses/` | Export query + handler (courses data) |
| `src/Terminar.Modules.Registrations/Application/Queries/ExportCourseRoster/` | Export query + handler (participants) |
| `src/Terminar.Api/Services/CsvExportService.cs` | CSV serialization using CsvHelper |
| `src/Terminar.Api/Modules/CoursesModule.cs` | New export endpoints registered here |
| `frontend/src/features/courses/ExportCoursesModal.tsx` | Export options UI component |
| `frontend/src/lib/download.ts` | Binary download helper utility |
