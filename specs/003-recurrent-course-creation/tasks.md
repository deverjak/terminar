# Tasks: Recurrent Course Creation

**Input**: Design documents from `/specs/003-recurrent-course-creation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: No test tasks generated — not requested in spec.
**Scope**: Frontend-only. No backend changes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on other in-progress tasks)
- **[Story]**: Which user story this task belongs to (US1–US4 maps to spec.md priorities P1–P4)
- All paths are relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create new file structure and extend types before any feature code is written.

- [x] T001 Create component and utility directories: `frontend/src/features/courses/components/` and `frontend/src/features/courses/utils/` (mkdir only — no files yet)
- [x] T002 Add `RecurrenceRule`, `SessionPreviewEntry`, and `GeneratedSession` TypeScript interfaces to `frontend/src/features/courses/types.ts` per `data-model.md` (append to existing file — do not modify existing types)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core utilities and translations that every user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T003 Implement `generateSessions(rules: RecurrenceRule[]): GeneratedSession[]` pure function in `frontend/src/features/courses/utils/recurrenceEngine.ts` — includes first-occurrence calculation (advance from seriesStartDate until dayOfWeek matches), 7-day stepping, time component injection from HH:MM string, multi-rule merge and chronological sort, stable key generation (`${ruleId}-${index}`); no hard occurrence cap — the engine generates as many sessions as requested
- [x] T004 [P] Add all `recurrence.*` i18n keys to `frontend/src/shared/i18n/locales/en.json` under the `courses` key — English strings for: mode toggle labels, add/remove rule buttons, all rule form field labels, end condition options, large-count soft warning (shown when occurrences > 100), preview title/count/add-manual/duplicate-warning/empty/no-sessions-error, mode-switch confirmation modal title/message/confirm/cancel
- [x] T005 [P] Add matching Czech translations for all `recurrence.*` keys to `frontend/src/shared/i18n/locales/cs.json` — same structure as T004, translated into Czech

**Checkpoint**: Foundation ready — `generateSessions` is importable and translations exist for all new UI text.

---

## Phase 3: User Story 1 — Single Weekly Recurrence Pattern (Priority: P1) 🎯 MVP

**Goal**: Staff can configure one weekly recurrence rule and see a live preview of generated sessions. The course can be submitted with recurrence-generated sessions.

**Independent Test**: Create a course in recurrence mode with a single Monday 09:00 rule for 4 weeks starting 2026-04-07. Verify the preview shows exactly 4 sessions (Apr 7, Apr 14, Apr 21, Apr 28) and that submitting the form sends those 4 `SessionInput` entries to the backend.

- [x] T006 [US1] Create `RecurrenceRuleForm` component in `frontend/src/features/courses/components/RecurrenceRuleForm.tsx` — props: `rule: RecurrenceRule`, `onChange: (rule: RecurrenceRule) => void`, `onRemove: () => void`; fields: day-of-week `Select` (Mon–Sun options), start time `TimeInput`, series start date `DatePickerInput`, end condition `SegmentedControl` ("By count" / "By date"), occurrences `NumberInput` (min=1, no max, shown when end condition = count; display a soft warning using the `recurrence.largeCountWarning` i18n key when value exceeds 100), end date `DatePickerInput` (shown when end condition = date); all labels use i18n keys; inline validation on each required field
- [x] T007 [US1] Add recurrence mode state to `frontend/src/features/courses/CreateCoursePage.tsx`: `mode` state (`'manual' | 'recurrence'`), `rules` state (`RecurrenceRule[]` initialised with one empty rule), `useMemo` that calls `generateSessions(rules)` filtering to only fully-valid rules, `SegmentedControl` toggle above the sessions section switching between manual and recurrence UI
- [x] T008 [US1] Create `SessionPreviewPanel` component (display-only for this task) in `frontend/src/features/courses/components/SessionPreviewPanel.tsx` — props: `entries: SessionPreviewEntry[]`; renders a `Table` of entries sorted chronologically, each row shows: day of week, date, start time, source badge (Generated / Manual); shows total session count using i18n key; shows empty-state message when entries list is empty
- [x] T009 [US1] Integrate `SessionPreviewPanel` into `CreateCoursePage.tsx` when mode = recurrence, and wire recurrence-mode submission: when form is submitted in recurrence mode, map the non-deleted `previewList` entries to `SessionInput[]` (`scheduledAt.toISOString()`, `durationMinutes` from course-level field, optional `location`) and pass to existing `createCourse()` call; block submission when previewList has zero non-deleted entries

**Checkpoint**: User Story 1 is fully functional — staff can schedule a course via weekly recurrence and submit it.

---

## Phase 4: User Story 2 — Preview and Edit Sessions Before Submission (Priority: P2)

**Goal**: Staff can delete individual sessions, edit any session's date/time, see duplicate warnings, and get a confirmation prompt when switching modes.

**Independent Test**: Generate 5 sessions from a weekly rule. Delete session 3. Edit session 2's time to 30 minutes later. Verify the submitted payload contains exactly 4 sessions with the correct times. Then switch mode back to manual and confirm the modal appears.

- [x] T010 [US2] Add delete-session capability to `SessionPreviewPanel.tsx`: add `onDelete: (key: string) => void` prop; render a delete icon button per row; clicking calls `onDelete(entry.key)`; implement `sessionOverrides` state in `CreateCoursePage.tsx` as `Map<string, { isDeleted?: boolean; scheduledAt?: Date }>` and an `handleSessionDelete(key)` handler that sets `isDeleted: true` in the map; apply overrides when building the `previewList` in `useMemo`
- [x] T011 [US2] Add inline date/time edit to `SessionPreviewPanel.tsx`: add `onEdit: (key: string, scheduledAt: Date) => void` prop; render an edit button per row that toggles an inline `DateTimePicker` replacing the static date/time cell; on date change call `onEdit`; implement `handleSessionEdit(key, scheduledAt)` in `CreateCoursePage.tsx` that records the new `scheduledAt` in `sessionOverrides` map
- [x] T012 [US2] Implement duplicate detection in the `previewList` `useMemo` in `CreateCoursePage.tsx`: after merging generated+overrides+manualAdditions, compare each non-deleted entry's `scheduledAt` (date portion + HH:MM) against all others; set `isDuplicate: true` on any entry that shares an identical datetime with another non-deleted entry
- [x] T013 [US2] Show duplicate warning in `SessionPreviewPanel.tsx`: render a yellow warning `Badge` next to duplicate rows using the `recurrence.preview.duplicateWarning` i18n key; also show a dismissible `Alert` above the session list when any duplicates exist (fires even if user proceeds to submit)
- [x] T014 [US2] Implement mode-switch confirmation modal in `CreateCoursePage.tsx` using Mantine `Modal`: when switching manual→recurrence while manual sessions exist, or recurrence→manual while generated/edited sessions exist, open a confirmation modal with title/message/confirm/cancel i18n keys; on confirm: discard sessions and switch mode; on cancel: keep current mode and sessions unchanged

**Checkpoint**: Staff can fully curate the session list before submitting. Mode switches never silently discard data.

---

## Phase 5: User Story 3 — Multiple Recurrence Patterns per Week (Priority: P3)

**Goal**: Staff can add more than one recurrence rule per course (e.g. Mon + Thu), see all sessions merged chronologically, and remove individual rules.

**Independent Test**: Add two rules (Mon 07:00 × 4 weeks, Thu 18:00 × 4 weeks). Verify preview shows 8 sessions in correct chronological order. Remove the Thursday rule. Verify preview shows only 4 Monday sessions.

- [x] T015 [US3] Extend recurrence rule management in `CreateCoursePage.tsx` to support multiple `RecurrenceRuleForm` instances: replace single-rule state with `rules: RecurrenceRule[]` (already an array, but now render one `RecurrenceRuleForm` per entry using `rules.map()`); add "Add recurrence rule" button that appends a new empty `RecurrenceRule` to the array; wire each `RecurrenceRuleForm`'s `onRemove` to filter it from the array; update `useMemo` to call `generateSessions(rules)` across all rules (it already accepts an array — no change to the engine itself)
- [x] T016 [US3] Add per-rule incomplete-configuration error state to `RecurrenceRuleForm.tsx`: expose a `showErrors: boolean` prop; when true, mark missing required fields with Mantine error styling; in `CreateCoursePage.tsx` track which rules are complete (all required fields filled) and pass `showErrors={hasAttemptedPreview}` so inline errors appear only after the user has interacted with the form; sessions from incomplete rules are silently excluded from the preview (already handled by filtering in T003's engine)

**Checkpoint**: Courses with Mon/Wed/Fri (or any combination) scheduling can be fully configured in a single form.

---

## Phase 6: User Story 4 — Manual Session Addition to Supplement Recurrence (Priority: P4)

**Goal**: Staff can add one-off sessions that don't fit the recurrence pattern directly to the preview list.

**Independent Test**: Generate 3 sessions from a weekly rule. Click "Add session manually", enter a custom date/time, confirm. Verify the preview shows 4 sessions in chronological order and the submitted payload includes the manually added session.

- [x] T017 [US4] Add manual session addition to `SessionPreviewPanel.tsx` and `CreateCoursePage.tsx`: add "Add session manually" `Button` at the bottom of the preview panel (uses `recurrence.preview.addManual` i18n key); clicking opens an inline form (or small popover) with a `DateTimePicker`; on confirm call `onAddManual: (scheduledAt: Date) => void` prop; in `CreateCoursePage.tsx` maintain `manualAdditions: SessionPreviewEntry[]` state and implement `handleAddManual(scheduledAt)` that creates a new `SessionPreviewEntry` with `source: 'manual'`, `ruleId: null`, and appends it; include `manualAdditions` in the `useMemo` merge for `previewList`

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Tie up edge cases and verify the complete integrated experience.

- [x] T018 [P] Implement recurrence→manual mode switch "copy" behavior in `CreateCoursePage.tsx`: when the user confirms switching from recurrence mode to manual mode (via the modal from T014), instead of discarding sessions, convert all non-deleted `previewList` entries into `SessionFormValue` objects and load them into the existing Mantine form's `sessions` field — so the staff member retains their sessions in manual-edit mode
- [x] T019 Final end-to-end integration pass on `CreateCoursePage.tsx`: verify the complete submission path handles all combinations — recurrence-only sessions, mixed recurrence + manual additions, sessions with overrides (edits/deletes) — and that the `SessionInput[]` payload is always correctly shaped (`scheduledAt` as ISO string, `durationMinutes` from course-level input, `location` optional); fix any integration issues found

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1)**: Depends on Phase 2 — first deliverable (MVP)
- **Phase 4 (US2)**: Depends on Phase 3 (needs `SessionPreviewPanel` shell from T008)
- **Phase 5 (US3)**: Depends on Phase 2 only — T015 and T016 modify `CreateCoursePage.tsx` and `RecurrenceRuleForm.tsx` independently of Phase 4 changes (can run in parallel with Phase 4 if careful about merge conflicts in `CreateCoursePage.tsx`)
- **Phase 6 (US4)**: Depends on Phase 4 (`SessionPreviewPanel` with full props interface)
- **Phase 7 (Polish)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Foundational only — can start after Phase 2
- **US2 (P2)**: Depends on US1 (needs `SessionPreviewPanel` shell from T008, `sessionOverrides` state location in `CreateCoursePage.tsx`)
- **US3 (P3)**: Depends on Foundational only (multi-rule is an extension of the `rules` array already in `CreateCoursePage.tsx` from T007)
- **US4 (P4)**: Depends on US2 complete (adds to `SessionPreviewPanel` props interface finalized in T010/T011)

### Within Each Phase

- Models/types before components
- Engine (T003) before any component that imports it
- i18n keys (T004/T005) before any component that uses `useTranslation`
- `RecurrenceRuleForm` (T006) before it is rendered in `CreateCoursePage` (T007)
- `SessionPreviewPanel` shell (T008) before preview editing features (T010–T013)

### Parallel Opportunities

- T004 and T005 are fully parallel (different locale files)
- T010 and T011 are parallel within Phase 4 (touch different parts of `SessionPreviewPanel.tsx` — delete button vs. edit button — but both also modify `CreateCoursePage.tsx` so coordinate on that file)
- T015 and T016 are parallel within Phase 5
- T018 is parallel within Phase 7 (isolated to mode-switch branch in `CreateCoursePage.tsx`)

---

## Parallel Example: Phase 2

```
# T004 and T005 can run simultaneously (different files):
Task A: "Add recurrence i18n keys to frontend/src/shared/i18n/locales/en.json"
Task B: "Add Czech recurrence i18n keys to frontend/src/shared/i18n/locales/cs.json"
```

## Parallel Example: Phase 3 (after T006 completes)

```
# T007, T008 can run simultaneously (different files):
Task A: "Add mode toggle + rules state to CreateCoursePage.tsx"  (T007)
Task B: "Create SessionPreviewPanel display-only shell"           (T008)
# Then T009 depends on both completing
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T002)
2. Complete Phase 2: Foundational (T003–T005)
3. Complete Phase 3: User Story 1 (T006–T009)
4. **STOP and VALIDATE**: A staff member can create a course with a single weekly recurrence pattern and submit it successfully
5. Deploy/demo if ready — this alone eliminates ~90% of manual session entry for typical courses

### Incremental Delivery

1. Phase 1 + 2 → Recurrence engine ready
2. Phase 3 → MVP: single-rule recurrence + preview + submit ✓
3. Phase 4 → Add: delete/edit sessions, duplicate warning, mode-switch safety ✓
4. Phase 5 → Add: multiple rules per week ✓
5. Phase 6 → Add: one-off manual sessions in recurrence mode ✓
6. Phase 7 → Polish and edge cases ✓

### Single-Developer Sequential Order

T001 → T002 → T003 → T004+T005 → T006 → T007+T008 → T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016 → T017 → T018 → T019

---

## Notes

- All new components must import translation keys via `useTranslation()` — no hardcoded strings (Constitution Principle III)
- `recurrenceEngine.ts` must have zero React/Mantine imports — pure TypeScript only
- `CreateCoursePage.tsx` is the single stateful orchestrator; `RecurrenceRuleForm` and `SessionPreviewPanel` are controlled components receiving props
- The existing manual session entry UI (Mantine `useForm` list) remains unchanged in `manual` mode
- Commit after each task or logical group for clean git history
