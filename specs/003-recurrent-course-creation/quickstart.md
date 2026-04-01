# Quickstart: Recurrent Course Creation

**Branch**: `003-recurrent-course-creation`

## Overview

This feature extends the existing `CreateCoursePage` with a recurrence mode that generates session arrays automatically from user-configured weekly patterns, with a live editable preview before submission.

No backend changes. No new routes. No new API endpoints.

---

## Files to Create

```
frontend/src/features/courses/
├── components/
│   ├── RecurrenceRuleForm.tsx      # Single rule card (day, time, start date, end condition)
│   └── SessionPreviewPanel.tsx     # Editable session list with add/delete/edit/manual-add
└── utils/
    └── recurrenceEngine.ts         # Pure function: RecurrenceRule[] → GeneratedSession[]
```

---

## Files to Modify

```
frontend/src/features/courses/
├── CreateCoursePage.tsx            # Add mode toggle, rule state, preview integration
└── types.ts                        # Add RecurrenceRule, SessionPreviewEntry types

frontend/src/shared/i18n/
├── locales/en/courses.json         # Add ~20 new translation keys
└── locales/cs/courses.json         # Czech translations for same keys
```

---

## Key Integration Points

### 1. Mode Toggle in CreateCoursePage

```tsx
// New state in CreateCoursePage
const [mode, setMode] = useState<'manual' | 'recurrence'>('manual');
const [rules, setRules] = useState<RecurrenceRule[]>([]);
const [sessionOverrides, setSessionOverrides] = useState<Map<string, ...>>(new Map());
const [manualAdditions, setManualAdditions] = useState<SessionPreviewEntry[]>([]);

// Derived preview list (replaces the existing `form.values.sessions` in recurrence mode)
const previewList = useMemo(() => {
  const generated = generateSessions(rules);
  // apply overrides + merge manualAdditions + sort + flag duplicates
  ...
}, [rules, sessionOverrides, manualAdditions]);
```

### 2. Submission Conversion

```tsx
// Convert preview entries to SessionInput[] for the API
const sessions: SessionInput[] = previewList
  .filter(e => !e.isDeleted)
  .map(e => ({
    scheduledAt: e.scheduledAt.toISOString(),
    durationMinutes: e.durationMinutes,
    location: e.location || undefined,
  }));
```

### 3. recurrenceEngine Signature

```typescript
// frontend/src/features/courses/utils/recurrenceEngine.ts
export function generateSessions(rules: RecurrenceRule[]): GeneratedSession[]
```

---

## Running the App During Development

```bash
# Backend (from repo root)
cd src/Terminar.AppHost && dotnet run

# Frontend (from repo root)
cd frontend && npm run dev
# → http://localhost:5173
```

Login as a staff user, navigate to any course list page, click "Create Course", and the mode toggle (Manual / Recurrent) should appear above the sessions section.

---

## Translation Key Locations

- English: `frontend/src/shared/i18n/locales/en/courses.json`
- Czech: `frontend/src/shared/i18n/locales/cs/courses.json`

New keys live under the `recurrence` namespace within those files. See `research.md` for the full list of required keys.
