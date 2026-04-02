# Data Model: Course Enrollment Email and Session Excusals

**Feature**: `004-enrollment-email-excusals`  
**Phase**: 1 — Design  
**Date**: 2026-04-02

---

## Module Overview

| Entity / Concept | Module | Schema | New / Modified |
|---|---|---|---|
| `Registration` | Registrations | `registrations` | Modified (rename SafeLinkToken) |
| `Excusal` | Registrations | `registrations` | New aggregate |
| `ExcusalCredit` | Registrations | `registrations` | New aggregate |
| `ExcusalCreditAuditEntry` | Registrations | `registrations` | New entity |
| `ParticipantMagicLink` | Registrations | `registrations` | New aggregate |
| `CourseExcusalPolicy` | Courses | `courses` | New owned entity on Course |
| `ExcusalValidityWindow` | Tenants | `tenants` | New aggregate |
| `TenantExcusalSettings` | Tenants | `tenants` | New owned entity on Tenant |

---

## Registrations Module

### Registration (modified)

```csharp
// Existing aggregate — one field renamed, no structural change
class Registration : AggregateRoot<Guid>
{
    TenantId TenantId
    Guid CourseId
    string ParticipantName
    Email ParticipantEmail
    RegistrationSource RegistrationSource
    Guid? RegisteredByStaffId
    RegistrationStatus Status           // Confirmed | Cancelled
    DateTimeOffset RegisteredAt
    DateTimeOffset? CancelledAt
    string? CancellationReason
    Guid SafeLinkToken                  // RENAMED from SelfCancellationToken
                                        // Token enabling all participant self-service actions
                                        // Permanent while enrollment is active
}
```

**Migration note**: `SelfCancellationToken` column renamed to `SafeLinkToken` in `registrations.registrations` table.

---

### Excusal (new aggregate)

Represents an absence record — a participant excused themselves from a specific session.

```csharp
class Excusal : AggregateRoot<Guid>
{
    TenantId TenantId
    Guid RegistrationId                 // FK to Registration (by value, no nav)
    Guid CourseId                       // FK to Course session's parent course
    Guid SessionId                      // FK to Course.Session
    Email ParticipantEmail              // Denormalized for query convenience
    string ParticipantName              // Denormalized for reporting
    DateTimeOffset CreatedAt
    ExcusalStatus Status                // Recorded | CreditIssued
    Guid? ExcusalCreditId               // Set when credit is issued
}

enum ExcusalStatus { Recorded, CreditIssued }
```

**Invariants**:
- One excusal per (RegistrationId, SessionId) — duplicate submissions rejected.
- Cannot be created after the excusal deadline (hours-before-session configured in TenantExcusalSettings / CourseExcusalPolicy).
- Cannot be created for a past session.

**Domain events**:
- `ExcusalCreated(ExcusalId, RegistrationId, CourseId, SessionId, TenantId, OccurredAt)`

---

### ExcusalCredit (new aggregate)

A redeemable credit issued when a participant excuses from a session on a credit-generating course.

```csharp
class ExcusalCredit : AggregateRoot<Guid>
{
    TenantId TenantId
    Email ParticipantEmail              // Immutable — cannot be changed by staff
    string ParticipantName
    Guid SourceExcusalId                // FK to Excusal
    Guid SourceCourseId
    Guid SourceSessionId
    List<string> Tags                   // Inherited from CourseExcusalPolicy.Tags at creation time
                                        // Staff-replaceable (full replacement only)
    List<Guid> ValidWindowIds           // Sorted list of ExcusalValidityWindow IDs
                                        // Set at creation: source window + N subsequent windows
    ExcusalCreditStatus Status          // Active | Redeemed | Expired | Cancelled
    DateTimeOffset CreatedAt
    DateTimeOffset? RedeemedAt
    Guid? RedeemedCourseId              // Course enrolled in upon redemption
    DateTimeOffset? DeletedAt           // Soft delete timestamp (permanent, no restore)
}

enum ExcusalCreditStatus { Active, Redeemed, Expired, Cancelled }
```

**Invariants**:
- `Tags` must not be empty (validation error if staff tries to set empty tag list).
- `ValidWindowIds` must reference valid `ExcusalValidityWindow` IDs within the same tenant.
- Only `Active` credits can be redeemed or extended or re-tagged.
- Only `Active` credits can be soft-deleted.
- Soft delete is permanent (`DeletedAt` is set, `Status` → `Cancelled`; no restore).
- Redemption is single-use: once `Redeemed`, no further changes.
- Extension: staff can only append windows that come after the current last window.

**Domain events**:
- `ExcusalCreditIssued(CreditId, TenantId, ParticipantEmail, SourceExcusalId, OccurredAt)`
- `ExcusalCreditRedeemed(CreditId, TenantId, RedeemedCourseId, OccurredAt)`
- `ExcusalCreditCancelled(CreditId, TenantId, OccurredAt)`

**Staff methods**:
- `Extend(additionalWindowIds)` — appends new valid window IDs; audit-logged.
- `ReTag(newTags)` — fully replaces tag set; audit-logged. Rejects empty list.
- `SoftDelete(actorStaffId)` — sets `DeletedAt`, `Status = Cancelled`; audit-logged.

---

### ExcusalCreditAuditEntry (new entity, owned by ExcusalCredit)

Immutable log entry for each staff mutation on an ExcusalCredit.

```csharp
class ExcusalCreditAuditEntry : Entity<Guid>
{
    Guid ExcusalCreditId
    Guid ActorStaffId
    ExcusalCreditActionType ActionType  // Extend | ReTag | SoftDelete
    string FieldChanged                 // e.g., "ValidWindowIds", "Tags", "Status"
    string PreviousValue                // JSON-serialized previous value
    string NewValue                     // JSON-serialized new value
    DateTimeOffset Timestamp
}

enum ExcusalCreditActionType { Extend, ReTag, SoftDelete }
```

**Notes**: Owned collection on `ExcusalCredit`. Entries are append-only; no update or delete.

---

### ParticipantMagicLink (new aggregate)

Manages the passwordless magic link flow for the participant portal.

```csharp
class ParticipantMagicLink : AggregateRoot<Guid>
{
    TenantId TenantId
    Email ParticipantEmail
    string MagicLinkToken               // 32-byte URL-safe base64, single-use
    DateTimeOffset MagicLinkExpiresAt   // Now + 15 minutes
    DateTimeOffset? MagicLinkUsedAt     // Set on redemption; null = not yet used

    string? PortalToken                 // 32-byte URL-safe base64, multi-use within TTL
    DateTimeOffset? PortalTokenExpiresAt // Now + 7 days (set on redemption)
}
```

**Invariants**:
- A new magic link request for the same email+tenant ALWAYS creates a new record (previous unredeemed tokens remain valid but only one link is sent at a time in practice; no rate limiting in v1 — deferred).
- Magic link is single-use: once `MagicLinkUsedAt` is set, subsequent requests with the same token are rejected.
- Portal token is multi-use within TTL. After expiry, participant must request a new magic link.
- Only created when at least one active registration exists for the email+tenant.

**Methods**:
- `Redeem()` — sets `MagicLinkUsedAt`, generates `PortalToken` + `PortalTokenExpiresAt`.
- `static ValidatePortalToken(token)` — queries by portal token, checks expiry.

---

## Courses Module

### CourseExcusalPolicy (new owned entity on Course)

Per-course excusal configuration. Owned entity stored with Course.

```csharp
class CourseExcusalPolicy   // EF Core owned entity
{
    bool? CreditGenerationOverride      // null = use tenant default; true/false = override
    Guid? ValidityWindowId              // Assigned ExcusalValidityWindow; null = not set
    List<string> Tags                   // Freeform strings; must be non-empty for credits to be generated
}
```

**Rules**:
- If `CreditGenerationOverride == null` → use `TenantExcusalSettings.CreditGenerationEnabled`.
- If `Tags` is empty AND effective credit generation is enabled → validation error on attempt to generate credit (not on policy save).
- `ValidityWindowId` must reference an existing `ExcusalValidityWindow` in the same tenant.

**Effective credit generation** (resolved at runtime):
```
effectiveEnabled = CreditGenerationOverride ?? TenantExcusalSettings.CreditGenerationEnabled
canGenerate = effectiveEnabled && Tags.Any() && ValidityWindowId.HasValue
```

---

## Tenants Module

### TenantExcusalSettings (new owned entity on Tenant)

Tenant-wide defaults for excusal behaviour.

```csharp
class TenantExcusalSettings  // EF Core owned entity on Tenant
{
    bool CreditGenerationEnabled        // Default: false (opt-in)
    int ForwardWindowCount              // Default: 2 (credit valid for source window + 2 more)
    int UnenrollmentDeadlineDays        // Default: 14 (days before first session)
    int ExcusalDeadlineHours            // Default: 24 (hours before each session)
}
```

---

### ExcusalValidityWindow (new aggregate)

A named time period used to scope excusal credit validity.

```csharp
class ExcusalValidityWindow : AggregateRoot<Guid>
{
    TenantId TenantId
    string Name                         // e.g., "Q1/2026", "Summer 2026"
    DateOnly StartDate
    DateOnly EndDate
    DateTimeOffset CreatedAt
    DateTimeOffset? DeletedAt           // Soft delete (cannot delete if referenced by active credits)
}
```

**Invariants**:
- `EndDate` must be after `StartDate`.
- Name must be unique within tenant.
- Cannot be deleted if any active `ExcusalCredit` references this window in its `ValidWindowIds`.
- Ordered by `StartDate` ascending when listed.

---

## State Transition Diagrams

### ExcusalCredit Status

```
                    ┌─────────┐
    [issued]        │         │
  ─────────────────►│  Active │
                    │         │
                    └────┬────┘
                         │
           ┌─────────────┼─────────────┐
           │             │             │
    [redemption]   [expiry check]  [staff soft-delete]
           │             │             │
           ▼             ▼             ▼
       ┌────────┐   ┌─────────┐  ┌───────────┐
       │Redeemed│   │ Expired │  │ Cancelled │
       └────────┘   └─────────┘  └───────────┘
          (terminal)  (terminal)   (terminal, soft-deleted)
```

### ParticipantMagicLink Flow

```
[magic link requested]
        │
        ▼
   MagicLinkToken set, MagicLinkExpiresAt = +15min
        │
        ▼  [participant clicks link within 15min]
   MagicLinkUsedAt set, PortalToken generated, PortalTokenExpiresAt = +7days
        │
        ▼  [participant uses portal within 7 days]
   PortalToken valid → return participant data
        │
        ▼  [after 7 days]
   PortalToken expired → participant must request new magic link
```

---

## EF Core Configuration Notes

- `ExcusalCreditAuditEntry` is an owned collection on `ExcusalCredit` (EF owns, no separate repository needed; appended via `ExcusalCredit.Extend/ReTag/SoftDelete` methods).
- `CourseExcusalPolicy.Tags` stored as `text[]` (PostgreSQL native array) via value conversion.
- `ExcusalCredit.Tags` stored as `text[]` similarly.
- `ExcusalCredit.ValidWindowIds` stored as `uuid[]`.
- `ParticipantMagicLink` has a unique index on `(TenantId, MagicLinkToken)` and `(TenantId, PortalToken)`.
- `ExcusalValidityWindow` has a unique index on `(TenantId, Name)`.
- All new aggregates have a global query filter on `TenantId` (multi-tenancy enforcement).

---

## Database Migration Summary

New tables in `registrations` schema:
- `excusals`
- `excusal_credits`
- `excusal_credit_audit_entries`
- `participant_magic_links`

New columns in `registrations.registrations`:
- `safe_link_token` (rename from `self_cancellation_token`, same type `uuid`)

New owned columns in `courses.courses` (or `courses.course_excusal_policies`):
- `excusal_credit_generation_override` (`bool?`)
- `excusal_validity_window_id` (`uuid?`)
- `excusal_tags` (`text[]`)

New tables in `tenants` schema:
- `excusal_validity_windows`

New owned columns in `tenants.tenants`:
- `excusal_credit_generation_enabled` (`bool`, default `false`)
- `excusal_forward_window_count` (`int`, default `2`)
- `excusal_unenrollment_deadline_days` (`int`, default `14`)
- `excusal_deadline_hours` (`int`, default `24`)
