# Research: Recurrent Course Creation

**Branch**: `003-recurrent-course-creation` | **Phase**: 0

## 1. Existing Frontend Architecture

### Decision: Extend CreateCoursePage, don't replace it
**Rationale**: The current `CreateCoursePage.tsx` already manages session state via Mantine's `useForm` with `insertListItem` / `removeListItem`. The recurrence feature adds a mode layer on top of that existing form, not a separate form. Sessions ultimately flow into the same `sessions: SessionInput[]` array submitted to the backend.

**Alternatives considered**: Separate route/page for "recurrent course creation" — rejected because it duplicates all the course metadata fields and creates unnecessary navigation complexity.

---

## 2. Session Payload Contract (confirmed, no backend changes needed)

**Decision**: The existing `SessionInput` type is sufficient. No backend API changes are required.

```typescript
interface SessionInput {
  scheduledAt: string;   // ISO 8601 datetime — e.g. "2026-04-07T09:00:00.000Z"
  durationMinutes: number;
  location?: string;
}
```

**Rationale**: The spec assumption is confirmed by reading `types.ts`. The backend receives a flat array of sessions. The recurrence engine's only job is to produce this array from rule configuration. The existing `createCourse` API call is unchanged.

---

## 3. Recurrence Engine Design

**Decision**: A pure utility function (`recurrenceEngine.ts`) with no side effects.

```
recurrenceEngine(rules: RecurrenceRule[]): GeneratedSession[]
```

**Algorithm for each rule**:
1. Determine the first occurrence: find the first date on or after `seriesStartDate` whose weekday matches `dayOfWeek`.
2. Add sessions by advancing 7 days for each occurrence (or until `endDate` is reached when using end-date mode).
3. Merge all rules' sessions into a single array and sort chronologically.

**Rationale**: A pure function is trivial to unit test, has no dependencies on React or Mantine, and can be called on every form field change to keep the preview live. No external date library is needed — the existing `Date` API covers weekday arithmetic.

**Alternatives considered**: Using a third-party recurrence library (e.g., `rrule`) — rejected because the spec's requirements are limited to weekly recurrence with a fixed interval, which is a ~30-line algorithm. Adding a dependency for this simple case adds bundle weight with no benefit.

---

## 4. Real-Time Preview Update Strategy

**Decision**: Derive the session preview directly from recurrence rule form state on every render — no manual "generate" button.

**Rationale**: The spec's FR-005 requires automatic real-time updates. Since `recurrenceEngine` is a pure function and runs in-memory, it can be called synchronously on every render inside a `useMemo` hook. Performance is not a concern: even at the maximum of 3 rules × 52 sessions = 156 sessions, date arithmetic completes in microseconds.

---

## 5. Session Preview State Model

**Decision**: Session preview entries are managed in two layers:
1. **Generated sessions**: Derived from recurrence rules via `useMemo` — never stored in state directly.
2. **Override map**: A `Map<index, Partial<SessionInput>>` or array of per-session edits/deletions stored in `useState` — applied on top of generated sessions at render time.

When the user edits or deletes a session, the change is recorded in the override map. When recurrence rules change, the generated sessions list updates; overrides keyed by index may be invalidated (this is documented as an edge case behavior — rule changes reset any existing session overrides after a confirmation prompt).

**Simpler alternative considered**: A single flat `SessionFormValue[]` array in state that is both generated and editable — accepted for the "manual additions" case but conflicts with real-time rule-driven updates, because every rule change would need to surgically merge with existing edits. The override map keeps edits stable while allowing rule-driven regeneration.

---

## 6. Component Decomposition

**Decision**: Three new components within `frontend/src/features/courses/`:

| Component | Responsibility |
|-----------|---------------|
| `components/RecurrenceRuleForm.tsx` | Single rule card: day-of-week picker, time picker, start date, end condition (occurrences OR end date) |
| `components/SessionPreviewPanel.tsx` | Editable session list: delete row, edit date/time inline, add manual session, show total count, duplicate warnings |
| `utils/recurrenceEngine.ts` | Pure function: `RecurrenceRule[] → GeneratedSession[]` |

The mode toggle (Manual vs. Recurrence) is an inline segment control in `CreateCoursePage` itself, not a separate component.

---

## 7. i18n Requirements

**Decision**: All new user-facing strings must be added to both `en` and `cs` translation files.

New translation keys needed (namespace: `courses`):

- `recurrence.modeToggle.manual`
- `recurrence.modeToggle.recurrent`
- `recurrence.addRule`
- `recurrence.removeRule`
- `recurrence.dayOfWeek.label`
- `recurrence.startTime.label`
- `recurrence.seriesStartDate.label`
- `recurrence.endCondition.label`
- `recurrence.endCondition.byCount`
- `recurrence.endCondition.byDate`
- `recurrence.occurrences.label`
- `recurrence.endDate.label`
- `recurrence.preview.title`
- `recurrence.preview.sessionCount`
- `recurrence.preview.addManual`
- `recurrence.preview.duplicateWarning`
- `recurrence.preview.empty`
- `recurrence.switchToManual.confirmTitle`
- `recurrence.switchToManual.confirmMessage`
- `recurrence.switchToManual.confirm`
- `recurrence.switchToManual.cancel`
- Validation error keys for each required field

**Rationale**: Constitution Principle III (Multi-Language First) — no hardcoded user-facing strings permitted.

---

## 8. Duplicate Session Detection

**Decision**: Detect duplicates at preview render time by comparing `(scheduledAt date string, startTime)` pairs. Highlight duplicate rows with a Mantine `Badge` or warning color. Allow submission but surface a dismissible `Alert` above the submit button.

---

## 9. Max Occurrences Guard

**Decision**: UI-level enforcement via `NumberInput` `max={52}` attribute and form validation. No backend enforcement needed (the spec confirmed no backend changes are in scope).

---

## Resolved Unknowns

| Unknown | Resolution |
|---------|-----------|
| Does backend need changes? | No — existing `SessionInput[]` array accepted as-is |
| Date arithmetic library needed? | No — native `Date` API sufficient for weekly-only recurrence |
| How to handle mode switch data loss? | Mantine `Modal` confirmation dialog before discarding sessions |
| Where does form state live? | In `CreateCoursePage` via `useState` for rule list + override map |
| Recurrence frequency options | Weekly only (v1 scope per spec) |
