# Implementation Plan: Recurrent Course Creation

**Branch**: `003-recurrent-course-creation` | **Date**: 2026-04-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-recurrent-course-creation/spec.md`

## Summary

Extend the existing `CreateCoursePage` with a recurrence mode that lets staff configure weekly session patterns (one or more per week), generates a live editable session preview, and submits the final session array in the existing backend API format. No backend changes required. Pure frontend work in React 19 + Mantine v9 + TypeScript.

## Technical Context

**Language/Version**: TypeScript 5.x, C# 14 / .NET 10
**Primary Dependencies**: React 19, Mantine v9, TanStack Query v5, react-i18next, React Router v7, Vite 6
**Storage**: No new storage — sessions are transient form state submitted to the existing backend API; backend uses PostgreSQL via EF Core
**Testing**: Vitest (frontend unit tests for recurrence engine pure function)
**Target Platform**: Web browser (desktop-first, same as existing frontend)
**Project Type**: Web application (React SPA + ASP.NET Core minimal API backend)
**Performance Goals**: Preview updates within 1 second of rule changes; handles up to 156 sessions (3 rules × 52 occurrences) without UI lag
**Constraints**: No backend API changes; weekly-only recurrence (v1); 52 occurrences max per rule
**Scale/Scope**: Single page extension; 3 new components; 1 utility module; ~20 new i18n keys per language

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Domain-Driven Design** | ✅ PASS | Frontend-only feature. No backend domain layer changes. Existing DDD structure is untouched. |
| **II. Multi-Tenancy by Default** | ✅ PASS | Tenant context is already threaded through authentication. Course creation is already tenant-scoped. No new tenant-sensitive code paths introduced. |
| **III. Multi-Language First** | ✅ PASS (with action) | All new UI strings are mapped to i18n keys. Czech and English translations are required tasks in the implementation. No hardcoded strings. |
| **IV. Clean Architecture** | ✅ PASS | Frontend-only. The recurrence engine is a pure utility function — no framework coupling. Component → util dependency is clean. |
| **V. Spec-First Development** | ✅ PASS | Approved `spec.md` exists. Plan is derived from spec. |

**Post-design re-check**: All principles continue to pass. The override map and preview derivation pattern follow unidirectional data flow; no new cross-layer dependencies are introduced.

## Project Structure

### Documentation (this feature)

```text
specs/003-recurrent-course-creation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code Changes

```text
frontend/src/features/courses/
├── CreateCoursePage.tsx            ← MODIFY: add mode toggle, recurrence state, preview integration
├── types.ts                        ← MODIFY: add RecurrenceRule, SessionPreviewEntry, GeneratedSession
├── components/                     ← NEW directory
│   ├── RecurrenceRuleForm.tsx      ← NEW: single rule card
│   └── SessionPreviewPanel.tsx     ← NEW: editable session list
└── utils/                          ← NEW directory
    └── recurrenceEngine.ts         ← NEW: pure session generation function

frontend/src/shared/i18n/locales/
├── en.json                         ← MODIFY: add ~20 recurrence keys
└── cs.json                         ← MODIFY: Czech translations for same keys
```

**Structure Decision**: Web application option (Option 2 from template). Frontend SPA in `frontend/`, backend unmodified. Feature is colocated with the existing `courses` feature module following the established feature-oriented pattern.

## Phase 0: Research

**Status**: ✅ Complete — see [research.md](research.md)

**Key decisions**:
- No backend API changes; existing `SessionInput[]` payload used as-is
- No third-party date/recurrence library; native `Date` API is sufficient for weekly recurrence
- Recurrence engine as a pure utility function called inside `useMemo`
- Override map pattern to preserve per-session edits across rule changes
- Mantine `Modal` confirmation for mode-switch data loss scenarios

## Phase 1: Design & Contracts

**Status**: ✅ Complete

### Artifacts

- **[data-model.md](data-model.md)**: Frontend types (`RecurrenceRule`, `SessionPreviewEntry`, `GeneratedSession`), `recurrenceEngine` interface, state relationships diagram
- **[quickstart.md](quickstart.md)**: Files to create/modify, integration points, dev setup
- **Contracts**: No new external API contracts — the feature uses the existing `POST /api/v1/courses/` endpoint with unchanged payload shape. Backend contract reference is in `data-model.md`.

### Component Architecture

```
CreateCoursePage
│
├── [mode toggle] SegmentedControl: "Manual" | "Recurrent"
│
├── [mode = manual] → existing Mantine form list (unchanged)
│
└── [mode = recurrence]
    ├── RecurrenceRuleForm (×N rules)     — add/remove rules
    └── SessionPreviewPanel               — live preview, edit, delete, add manual
         └── derives from: generateSessions(rules) + overrides + manualAdditions
```

### `recurrenceEngine` Algorithm

1. For each `RecurrenceRule` (only fully valid rules are processed):
   a. Find first occurrence: advance day-by-day from `seriesStartDate` until `date.getDay() === dayOfWeek`
   b. Generate occurrences: loop N times (or until `endDate`) adding 7 days each iteration
   c. Set time: parse `startTime` as HH:MM; set hours and minutes on each `Date` object
2. Flatten all rules' sessions into one array
3. Sort ascending by `scheduledAt`
4. Return with stable keys: `${ruleId}-${index}`

### Session Preview State

```
previewList (derived in useMemo):
  = generateSessions(rules)            // base generated sessions
    |> apply sessionOverrides          // per-session edits / soft-deletes
    |> concat manualAdditions          // one-off manually added sessions
    |> sort by scheduledAt ASC
    |> flag isDuplicate (same date+time as another non-deleted entry)
```

### Mode Switch Behavior

| From | To | Sessions exist? | Behavior |
|------|----|----------------|---------|
| manual | recurrence | No | Immediate switch |
| manual | recurrence | Yes | Confirmation modal → discard manual entries |
| recurrence | manual | No generated sessions | Immediate switch |
| recurrence | manual | Sessions generated | Confirmation modal → copy visible sessions to manual list |

### i18n Keys Required

All keys in namespace root of translation files (`en.json` / `cs.json`), under a `recurrence` parent key:

```
recurrence.modeToggle.manual
recurrence.modeToggle.recurrent
recurrence.addRule
recurrence.removeRule
recurrence.dayOfWeek.label
recurrence.startTime.label
recurrence.seriesStartDate.label
recurrence.endCondition.label
recurrence.endCondition.byCount
recurrence.endCondition.byDate
recurrence.occurrences.label
recurrence.occurrences.max
recurrence.endDate.label
recurrence.preview.title
recurrence.preview.sessionCount
recurrence.preview.addManual
recurrence.preview.duplicateWarning
recurrence.preview.empty
recurrence.preview.noSessionsError
recurrence.switchMode.confirmTitle
recurrence.switchMode.confirmMessage
recurrence.switchMode.confirm
recurrence.switchMode.cancel
```

## Complexity Tracking

No constitution violations. No complexity justification required.
