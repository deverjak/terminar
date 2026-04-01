# Data Model: Recurrent Course Creation

**Branch**: `003-recurrent-course-creation` | **Phase**: 1

> **Scope note**: This feature is entirely frontend. No new backend entities or database migrations are introduced. The data model below describes frontend form state types only.

---

## Frontend-Only Types

### `RecurrenceRule`

Represents a single recurrence pattern configured by the user in the form.

```
RecurrenceRule {
  id: string                        // Local UUID for React key stability
  dayOfWeek: 0–6                    // 0=Sunday … 6=Saturday (matches JS Date.getDay())
  startTime: string                 // "HH:MM" — 24-hour local time
  seriesStartDate: Date | null      // First date from which occurrences are calculated
  endCondition: "count" | "date"    // How the series terminates
  occurrences: number               // 1–52, used when endCondition = "count"
  endDate: Date | null              // Used when endCondition = "date"
}
```

**Validation rules**:
- `dayOfWeek` is required
- `startTime` is required, must be valid HH:MM
- `seriesStartDate` is required
- When `endCondition = "count"`: `occurrences` is required, min 1, max 52
- When `endCondition = "date"`: `endDate` is required, must be >= `seriesStartDate`

---

### `SessionPreviewEntry`

An entry in the editable session preview list, derived from recurrence rules or added manually.

```
SessionPreviewEntry {
  key: string                       // Stable local ID for React rendering
  scheduledAt: Date                 // The computed or manually set session datetime
  durationMinutes: number           // Inherited from course-level field
  location: string                  // Inherited from course-level default or overridden
  source: "recurrence" | "manual"  // How this entry was created
  ruleId: string | null            // Which RecurrenceRule generated this (null if manual)
  isDeleted: boolean               // Soft-deleted in preview (excluded from submission)
  isDuplicate: boolean             // Computed flag — same scheduledAt as another entry
}
```

**Derivation rules**:
- Generated entries: created by `recurrenceEngine(rules)` using `RecurrenceRule[]`
- Manual entries: created by the "Add session manually" action
- `isDuplicate`: true if another non-deleted entry in the list shares the same date+time
- `isDeleted`: toggled by the user's delete action; deleted entries are excluded from the submission payload

---

### `RecurrenceModeState`

Top-level form mode controlling which session entry UI is shown.

```
RecurrenceModeState: "manual" | "recurrence"
```

**State transition**:
- `manual → recurrence`: Immediate if the manual sessions list is empty; requires confirmation modal if sessions already exist.
- `recurrence → manual`: Requires confirmation modal if recurrence rules have generated sessions; transitions by copying visible (non-deleted) sessions into manual entry list.

---

## Existing Types (unchanged, for reference)

### `SessionInput` (backend contract)

```typescript
// Already defined in frontend/src/features/courses/types.ts
interface SessionInput {
  scheduledAt: string;   // ISO 8601, e.g. "2026-04-07T09:00:00.000Z"
  durationMinutes: number;
  location?: string;
}
```

The submission step converts `SessionPreviewEntry[]` → `SessionInput[]` by:
1. Filtering out `isDeleted = true` entries
2. Mapping `scheduledAt: Date` → `scheduledAt.toISOString()`
3. Including `durationMinutes` and `location` from each entry

---

## State Relationships

```
CreateCoursePage state
├── mode: RecurrenceModeState
├── recurrenceRules: RecurrenceRule[]        (managed in state)
│
├── [derived] generatedEntries: SessionPreviewEntry[]
│     └── produced by: recurrenceEngine(recurrenceRules) — useMemo
│
├── sessionOverrides: Map<key, Partial<SessionPreviewEntry>>
│     └── stores per-session edits and deletes applied on top of generatedEntries
│
├── manualAdditions: SessionPreviewEntry[]   (manual one-off sessions)
│
└── [derived] previewList: SessionPreviewEntry[]
      = merge(generatedEntries + overrides, manualAdditions)
        sorted by scheduledAt ASC
        with isDuplicate flags computed
```

---

## `recurrenceEngine` Interface

```typescript
// Input
interface RecurrenceRule {
  id: string;
  dayOfWeek: number;          // 0–6
  startTime: string;          // "HH:MM"
  seriesStartDate: Date;
  endCondition: 'count' | 'date';
  occurrences?: number;
  endDate?: Date;
}

// Output
interface GeneratedSession {
  key: string;                // Stable: `${ruleId}-${index}`
  ruleId: string;
  scheduledAt: Date;
}

// Signature
function generateSessions(rules: RecurrenceRule[]): GeneratedSession[]
```

**Algorithm**:
1. For each rule, compute the first occurrence date: advance from `seriesStartDate` day-by-day until `date.getDay() === dayOfWeek`.
2. Generate subsequent occurrences by adding 7 days repeatedly until the end condition is met.
3. Set the time component: parse `startTime` ("HH:MM") and set hours/minutes on each occurrence `Date`.
4. Concatenate all rules' sessions and sort by `scheduledAt`.
5. Return the flat sorted array.
