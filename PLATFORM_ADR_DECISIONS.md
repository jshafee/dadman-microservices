# PLATFORM ADR Decisions

## ADR-001: Canonical Principal and Scope Propagation
**Status**: Accepted

**Decision**
- Adopt **Token Exchange issuing an internal JWT** as the canonical approach.
- External IdP token is validated at gateway, then exchanged for a short-lived internal platform token containing canonical claims:
  - `principal_id`
  - `tenant_id`
  - `application_id`
  - `permission_snapshot_version`

**Rationale**
- Produces consistent identity/scope semantics across HTTP and async workloads.
- Avoids implicit trust in mutable gateway headers across hops.
- Supports machine/service principals and user principals uniformly.

- Correlation and causation are propagated via HTTP headers and event envelope metadata, not JWT claims.

**Consequences**
- Introduces token exchange service/component and signing key management.
- Services trust only internal JWT for authorization context.
- Gateway enrichment headers become non-canonical diagnostics only.

---

## ADR-002: Tenant/Application Registration Authority
**Status**: Accepted

**Decision**
- Manage Tenants and Applications in a **separate Platform Registry service** (not IAM-owned tables).

**Rationale**
- Keeps IAM focused on principals/permissions.
- Enables future platform lifecycle capabilities (provisioning, app onboarding, quotas, lifecycle states) without coupling to IAM internals.

**Consequences**
- IAM references `TenantId`/`ApplicationId` as foreign identifiers from Registry.
- Registry emits lifecycle events consumed by IAM and other services.
- Additional service/database to operate.

---

## ADR-003: Canonical API Versioning
**Status**: Accepted

**Decision**
- Use **URL segment versioning** as canonical (`/api/{service}/v1/...`).
- Query-string `api-version` is not canonical and should not be primary contract.

**Rationale**
- Matches existing platform API style and gateway routing patterns.
- Improves discoverability and docs clarity.

**Consequences**
- Breaking changes ship under new URL segment (e.g., `v2`).
- Gateway routes and OpenAPI definitions are version-segment based.

---

## ADR-004: Authorization Snapshot Distribution
**Status**: Accepted

**Decision**
- Use **version+fetch** strategy, not heavy permission payload propagation.
- Tokens/events carry compact `permission_snapshot_version`; consumers fetch materialized permissions when required.

**Rationale**
- Keeps tokens/events small and stable.
- Avoids oversized events and stale embedded permission lists.

**Consequences**
- Requires low-latency authorization read model API/cache.
- Consumers must handle cache miss/staleness with retry/fetch logic.

---

## ADR-005: Document Ownership Model
**Status**: Accepted

**Decision**
- Document aggregate includes explicit polymorphic ownership fields:
  - `OwnerResourceType`
  - `OwnerResourceId`
- `ownerPrincipalId` may remain for convenience but is not sufficient alone.

**Rationale**
- Documents may be owned by user, org unit, workflow instance, or future platform entities.
- Prevents hard-coding ownership semantics to principals only.

**Consequences**
- ACL and query APIs must support owner-resource filters.
- Ownership transfer operations must validate target resource existence.

---

## ADR-006: Workflow Assignment Model
**Status**: Accepted

**Decision**
- Support **org unit/position/group-based assignment** as first-class, with runtime resolution to users for inbox projection.
- Do not restrict model to pre-resolved users only.

**Rationale**
- Preserves organizational intent and reduces reassignment churn during org changes.
- Aligns with directory-driven enterprise workflows.

**Consequences**
- Workflow runtime requires resolver/projection for candidate users.
- Task visibility recalculation needed when directory assignments change.

---

## ADR-007: ScopeContext in MassTransit Consumers
**Status**: Accepted

**Decision**
- Set `ScopeContext` via a **MassTransit consume filter** that reads canonical scope fields from the message envelope payload.

**Rationale**
- Ensures uniform context creation for all consumers.
- Avoids ad hoc per-consumer scope extraction.

**Consequences**
- Messages missing required scope are rejected/parked.
- Consume filter becomes mandatory shared infrastructure.

---

## ADR-008: Event Envelope Mapping to MassTransit
**Status**: Accepted

**Decision**
- Use a **wrapper message envelope in payload** as canonical event contract.
- MassTransit headers may carry duplicated correlation hints for transport diagnostics, but not as source of truth.

**Rationale**
- Prevents metadata loss across brokers/transports/tooling.
- Makes persisted/replayed events self-contained.

**Consequences**
- Slightly larger message payloads.
- Producers/consumers standardize on envelope schema and versioning.

---

## Non-Goals / Explicit Exclusions
- No office-automation-specific business rules in platform contracts.
- No shared database across platform services.
- No deviation from mandatory `TenantId` + `ApplicationId` scoping for relevant resources.
