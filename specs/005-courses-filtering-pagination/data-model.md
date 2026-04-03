# Data Model: Courses Filtering, Sorting & Temporal Views

## Backend DTO Extension

### `CourseListItem` (Application layer — `ListCoursesQuery.cs`)

**Current record**:
```
CourseListItem(Guid Id, string Title, string Description, CourseType CourseType,
               RegistrationMode RegistrationMode, int Capacity, CourseStatus Status,
               int SessionCount, DateTime? FirstSessionAt)
```

**Updated record** — add two new fields:
```
CourseListItem(Guid Id, string Title, string Description, CourseType CourseType,
               RegistrationMode RegistrationMode, int Capacity, CourseStatus Status,
               int SessionCount, DateTime? FirstSessionAt,
               DateTime? LastSessionEndsAt,    ← NEW
               List<string> Tags)              ← NEW
```

**Population in `ListCoursesHandler`**:
- `LastSessionEndsAt` = `c.Sessions.MaxBy(s => s.ScheduledAt)?.EndsAt`  
- `Tags` = `c.ExcusalPolicy.Tags` (already loaded, owned entity)

No new tables or migrations required — data is already persisted.

---

## Frontend Type Extension

### `CourseListItem` (Frontend type — `types.ts`)

**Add two fields** to the existing interface:
```typescript
lastSessionEndsAt: string | null;  // ISO 8601 UTC datetime
tags: string[];
```

---

## Frontend Derived Types

### `TemporalBucket`

A new union type computed at runtime from session dates:

```typescript
type TemporalBucket = 'all' | 'upcoming' | 'ongoing' | 'past';
```

Classification logic (in `useCoursesFilter.ts`):
- `upcoming`: `firstSessionAt` is null OR `new Date(firstSessionAt) > now`
- `ongoing`: `firstSessionAt <= now` AND (`lastSessionEndsAt` is null OR `new Date(lastSessionEndsAt) > now`)
- `past`: `new Date(lastSessionEndsAt) <= now` OR status is `Cancelled` or `Completed`

### `SortField`

```typescript
type SortField = 'title' | 'firstSessionAt' | 'capacity';
type SortDirection = 'asc' | 'desc';
```

### `CourseFilters`

State shape managed by `useCoursesFilter`:

```typescript
interface CourseFilters {
  temporalBucket: TemporalBucket;
  search: string;
  statuses: CourseStatus[];          // empty = all statuses
  courseType: CourseType | null;     // null = all types
  tags: string[];                    // empty = all tags
  sortField: SortField;
  sortDirection: SortDirection;
}
```

---

## No New Entities or Migrations

This feature adds no new database tables, columns, or EF Core migrations. All changes are:
1. DTO projection (handler computes two additional fields from already-loaded data)
2. Frontend filter/sort logic over the existing API response
