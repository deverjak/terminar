<!--
SYNC IMPACT REPORT
==================
Version change: (new) → 1.0.0
Modified principles: N/A (initial constitution)
Added sections:
  - Core Principles (I–V)
  - Multi-Tenancy Standards
  - Development Workflow
  - Governance
Removed sections: N/A
Templates requiring updates:
  ✅ .specify/templates/plan-template.md — Constitution Check section already present; DDD structure
     should be reflected when plans are generated (no structural change to template needed)
  ✅ .specify/templates/spec-template.md — no constitution-driven mandatory section changes
  ✅ .specify/templates/tasks-template.md — DDD task types (domain model, application service,
     repository interface) should be used in generated tasks; template is generic enough
Deferred TODOs:
  - TODO(RATIFICATION_DATE): exact project start date unknown; set to 2026-03-30 (today) as
    constitution is being authored on this date
-->

# Termínář Constitution

## Core Principles

### I. Domain-Driven Design (NON-NEGOTIABLE)

The backend MUST be structured around bounded contexts and a rich domain model.
Business logic MUST reside in the domain layer — entities, aggregates, value objects,
and domain events are first-class citizens.

- Aggregates enforce their own invariants; no external code bypasses aggregate boundaries.
- Domain objects MUST NOT depend on infrastructure concerns (databases, HTTP, queues).
- Application services orchestrate use cases; they delegate to the domain, they do not
  contain business logic themselves.
- Repository interfaces are defined in the domain layer; implementations live in
  the infrastructure layer.
- Domain events MUST be used to communicate state changes across bounded contexts.
- Ubiquitous language from the domain (e.g., Course, Session, Registration, Tenant)
  MUST be used consistently in code, specs, and documentation.

**Rationale**: DDD aligns the codebase with the business domain, making it easier to
generalize Termínář for new use cases without rewriting core logic. It also enforces
testability by keeping domain logic free of infrastructure coupling.

### II. Multi-Tenancy by Default

Every domain entity and aggregate MUST be scoped to a Tenant from day one.
Tenant isolation is a domain invariant, not an infrastructure concern.

- No query, command, or domain operation MAY access data belonging to a different
  Tenant under any circumstances.
- Tenant context MUST be established at the application boundary (e.g., request
  authentication) and threaded through to every domain operation.
- Adding a new Tenant MUST require no structural code changes — only data/configuration.

**Rationale**: Termínář is designed to serve multiple independent organizations.
Building tenancy in from day one avoids costly retrofitting and ensures isolation
guarantees are baked into the architecture.

### III. Multi-Language First

All user-facing content MUST be internationalizable. No hardcoded user-facing strings
are permitted anywhere in the codebase.

- Language resolution order: user preference → tenant default → system fallback.
- Course content (titles, descriptions) MUST support per-language variants.
- System interface strings MUST be managed via a translation mechanism, not hardcoded.
- At minimum two languages MUST be supported and verified before any release.

**Rationale**: Termínář is explicitly a multi-language system. Treating i18n as a
first-class requirement prevents expensive rework and ensures the system is usable
by diverse audiences from the first deployment.

### IV. Clean Architecture — Dependencies Point Inward

The codebase MUST follow layered architecture with strictly enforced dependency direction:

```
Infrastructure → Application → Domain
```

- Domain layer: entities, value objects, aggregates, domain events, repository interfaces.
  Zero external dependencies.
- Application layer: use case / command handlers, application services, DTOs.
  Depends only on Domain.
- Infrastructure layer: persistence, HTTP, email, messaging.
  Implements interfaces defined in Domain or Application.

Violations of this dependency rule (e.g., a domain object importing a database library)
MUST be treated as blocking defects and corrected before merge.

**Rationale**: Clean architecture enforces the DDD principle that domain logic is
independent of delivery mechanisms, enabling independent testing of each layer and
future substitution of infrastructure components.

### V. Spec-First Development

Every feature MUST have an approved specification (`spec.md`) before implementation
begins. Implementation plans and tasks are derived from specs, not the reverse.

- Specs are technology-agnostic — they describe WHAT users need, not HOW to build it.
- A plan (`plan.md`) MAY introduce technology choices after spec approval.
- No code SHOULD be written for a feature that lacks a spec, except exploratory
  spikes which MUST be discarded or converted to a proper spec before merging.

**Rationale**: Spec-first prevents scope creep, keeps business intent explicit, and
ensures the system evolves toward user needs rather than technical convenience.

## Multi-Tenancy Standards

- Each Tenant has a unique identifier that is part of every domain aggregate root.
- Staff users belong to exactly one Tenant and MUST NOT be able to act on another
  Tenant's data, even if they share infrastructure.
- Tenant configuration (default language, registration policies, branding) is managed
  independently per Tenant.
- System-level administration (creating/suspending Tenants) is a separate concern from
  Tenant-level staff operations and MUST be handled by a distinct administrative role.

## Development Workflow

- Features follow the speckit workflow: `/speckit.specify` → `/speckit.clarify`
  (optional) → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`.
- The Constitution Check gate in `plan.md` MUST verify compliance with Principles I–V
  before Phase 0 research proceeds.
- DDD structure in plans MUST reflect the four layers: Domain, Application,
  Infrastructure, and (where applicable) Presentation/API.
- Task lists MUST organize domain model tasks before application service tasks, and
  application service tasks before infrastructure tasks — this mirrors the dependency
  direction mandated by Principle IV.
- All PRs MUST verify that no domain layer file imports from application or
  infrastructure layers.

## Governance

- This constitution supersedes all other development practices and conventions.
  In the event of conflict, the constitution takes precedence.
- Amendments require: (1) a written rationale, (2) update to this file with version
  bump, (3) propagation check across all dependent templates and specs.
- Version bump rules:
  - **MAJOR**: removal or redefinition of an existing principle.
  - **MINOR**: new principle or section added; materially expanded guidance.
  - **PATCH**: clarifications, wording, or non-semantic refinements.
- Compliance review: every feature plan MUST include a Constitution Check section
  confirming adherence to active principles before implementation tasks begin.
- The constitution is the source of truth for architectural decisions. Individual
  implementation plans MUST NOT contradict it; if a plan requires a deviation,
  an amendment to the constitution MUST be proposed first.

**Version**: 1.0.0 | **Ratified**: 2026-03-30 | **Last Amended**: 2026-03-30
