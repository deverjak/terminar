# Quickstart Guide: Per-Participant Custom Field Values (006)

## Prerequisites

- Backend running via `cd src/Terminar.AppHost && dotnet run`
- Frontend running via `cd frontend && npm run dev`
- A tenant exists with at least one staff user authenticated
- At least one course exists with at least one confirmed registration

---

## End-to-End Walkthrough

### Step 1: Create Custom Field Definitions (Tenant Settings)

Navigate to **Tenant Settings → Custom Fields** in the frontend.

Create two fields:
1. Name: `Deposit Paid`, Type: `Yes/No`
2. Name: `Payment Status`, Type: `Options List`, Options: `None, Partial, Full`

Verify both appear in the field definitions list.

**API equivalent**:
```bash
# Create "Deposit Paid" field
curl -X POST http://localhost:5000/api/v1/settings/custom-fields \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Deposit Paid","fieldType":"YesNo","allowedValues":[]}'

# Create "Payment Status" field
curl -X POST http://localhost:5000/api/v1/settings/custom-fields \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Payment Status","fieldType":"OptionsList","allowedValues":["None","Partial","Full"]}'
```

---

### Step 2: Enable Fields on a Course

Open a course in the frontend and navigate to **Course Settings → Custom Fields**.

Enable `Deposit Paid` for the course. Leave `Payment Status` disabled.

Verify the course field assignment list shows `Deposit Paid = enabled`, `Payment Status = disabled`.

**API equivalent**:
```bash
# Enable "Deposit Paid" on course (replace UUIDs)
curl -X PUT http://localhost:5000/api/v1/courses/{courseId}/custom-fields \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"enabledFieldIds":["<depositPaidFieldId>"]}'
```

---

### Step 3: View and Edit Participant Field Values

Open the **Participant List** for the course.

Verify a `Deposit Paid` column appears with `—` (not set) for each participant.

Click the toggle for one participant → changes to `Yes` and saves immediately.

Reload the page. Verify the value persisted.

**API equivalent**:
```bash
# Get roster (includes customFieldValues in response)
curl http://localhost:5000/api/v1/courses/{courseId}/registrations \
  -H "Authorization: Bearer $TOKEN"

# Set "Deposit Paid = true" for a registration
curl -X PATCH http://localhost:5000/api/v1/courses/{courseId}/registrations/{registrationId}/field-values \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"fieldDefinitionId":"<depositPaidFieldId>","value":"true"}'
```

---

### Step 4: Verify Isolation

Enroll the same participant in a **second course**.

Open that second course's participant list.

Verify `Deposit Paid` column does **not** appear (field not enabled on course 2).

Enable `Deposit Paid` on course 2.

Verify the participant shows `—` (not set) — the value from course 1 did **not** carry over.

---

### Step 5: Delete a Field Definition

Navigate back to **Tenant Settings → Custom Fields**.

Delete `Deposit Paid`.

Open both courses' participant lists.

Verify the `Deposit Paid` column no longer appears on either course.

---

## Running EF Core Migrations

Three migrations must be applied (one per module):

```bash
# Tenants module
dotnet ef migrations add AddCustomFieldDefinitions \
  --project src/Terminar.Modules.Tenants \
  --startup-project src/Terminar.Api \
  --context TenantsDbContext

# Courses module
dotnet ef migrations add AddCourseFieldAssignments \
  --project src/Terminar.Modules.Courses \
  --startup-project src/Terminar.Api \
  --context CoursesDbContext

# Registrations module
dotnet ef migrations add AddParticipantFieldValues \
  --project src/Terminar.Modules.Registrations \
  --startup-project src/Terminar.Api \
  --context RegistrationsDbContext
```

Migrations are applied automatically on startup via Aspire.

---

## Key Files to Modify / Create

### Backend

| Action | File |
|--------|------|
| New entity | `src/Terminar.Modules.Tenants/Domain/CustomFieldDefinition.cs` |
| New entity | `src/Terminar.Modules.Courses/Domain/CourseFieldAssignment.cs` |
| New entity | `src/Terminar.Modules.Registrations/Domain/ParticipantFieldValue.cs` |
| Extend aggregate | `src/Terminar.Modules.Courses/Domain/Course.cs` |
| Extend aggregate | `src/Terminar.Modules.Registrations/Domain/Registration.cs` |
| New commands/queries | `src/Terminar.Modules.Tenants/Application/CustomFields/` |
| New commands/queries | `src/Terminar.Modules.Courses/Application/CustomFields/` |
| New commands/queries | `src/Terminar.Modules.Registrations/Application/CustomFields/` |
| New EF config | `src/Terminar.Modules.Tenants/Infrastructure/Configurations/CustomFieldDefinitionConfiguration.cs` |
| New EF config | `src/Terminar.Modules.Courses/Infrastructure/Configurations/CourseFieldAssignmentConfiguration.cs` |
| New EF config | `src/Terminar.Modules.Registrations/Infrastructure/Configurations/ParticipantFieldValueConfiguration.cs` |
| New API module | `src/Terminar.Api/Modules/CustomFieldsModule.cs` |
| Extend API module | `src/Terminar.Api/Modules/RegistrationsModule.cs` |
| Extend API module | `src/Terminar.Api/Modules/CoursesModule.cs` |

### Frontend

| Action | File |
|--------|------|
| New settings page | `frontend/src/features/settings/CustomFieldsSettingsPage.tsx` |
| New course section | `frontend/src/features/courses/CourseCustomFieldsSection.tsx` |
| Extend roster page | `frontend/src/features/registrations/CourseRosterPage.tsx` |
| New API client functions | `frontend/src/features/settings/customFieldsApi.ts` |
| Extend API functions | `frontend/src/features/registrations/registrationsApi.ts` |
| New translations | `frontend/src/shared/i18n/locales/en.json` (add `customFields.*` keys) |
| New translations | `frontend/src/shared/i18n/locales/cs.json` (add `customFields.*` keys) |
