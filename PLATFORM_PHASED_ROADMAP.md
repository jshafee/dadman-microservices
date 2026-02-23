# Platform Phased Roadmap

## Phase 1 — Foundation & Guardrails
**Goal**: Establish common platform scaffolding and non-functional standards.

**Scope**
- Service templates for Gateway, services, workers.
- Standardized TenantId/ApplicationId scope context.
- Token exchange baseline for internal JWT issuance and propagation.
- EF Core outbox/inbox baseline package usage.
- OTEL tracing/logging/metrics with Seq integration.
- API and event conventions (URL segment versioning, correlation IDs, error contracts).

**Acceptance criteria**
- All platform services compile with shared baseline packages and middleware.
- Request pipeline enforces and propagates TenantId/ApplicationId.
- Example end-to-end trace visible from gateway to one service and one event consumer.
- Architecture decision records approved for DB-per-service and event contract policy.

---

## Phase 2 — IAM + Organization Directory (Identity Core)
**Goal**: Deliver reusable identity/authorization and directory foundations.

**Scope**
- IAM service: users, groups, roles, permissions, service principals.
- Platform Registry service: tenant and application registration/lifecycle.
- Organization Directory: org units, positions, assignments, memberships.
- Authorization check API for platform and business services.
- Initial IAM + Org event publication.

**Acceptance criteria**
- CRUD APIs for IAM and Org contexts are available and documented.
- Effective permission evaluation works per tenant+application.
- Directory assignments can drive group membership projections.
- Audit records captured for admin/security mutations.

---

## Phase 3 — Document Management (Metadata + ACL + Object Store)
**Goal**: Provide shared document capability for all systems.

**Scope**
- Document metadata, versions, tags, ACL engine.
- Presigned upload/download flow with object storage integration.
- Access decision + explicit access event emission.
- Search/filter APIs on metadata (scoped by tenant/application).

**Acceptance criteria**
- Document binaries are stored outside relational DB.
- ACL updates enforceable and tested for user/group/role principals.
- Access operations emit audit-ready events (`Allowed` and `Denied`).
- Performance baseline met for metadata search at target scale.

---

## Phase 4 — Workflow Engine (Generic Approval/Task Inbox)
**Goal**: Introduce reusable workflow orchestration without business-specific rules.

**Scope**
- Workflow definitions/versioning and instance execution.
- Generic task model, inbox API, claim/reassign/complete flows.
- Assignment rules using IAM groups and directory positions.
- Event hooks for business apps to react to workflow lifecycle.

**Acceptance criteria**
- At least two generic approval patterns run from definition to completion.
- Inbox query supports assignee, group, due date, and status filters.
- Reassignment/escalation paths are auditable.
- No office-automation-specific domain logic in workflow service code/contracts.

---

## Phase 5 — Audit Trail + Notifications
**Goal**: Complete compliance and communication capabilities.

**Scope**
- Append-only audit store and event ingestion pipeline.
- Evidence export API with integrity hashes.
- Notification templates + worker-based delivery + retry tracking.
- Initial channels: email first, others pluggable.

**Acceptance criteria**
- Audit ingestion supports both bus events and explicit access APIs.
- Integrity checks validate append-only chain consistency.
- Notifications provide observable delivery states and dead-letter handling.
- Compliance queries return records by actor/action/resource/time range.

---

## Phase 6 — Hardening, Multi-App Adoption & Platform Operations
**Goal**: Prepare for broad reuse across Office Automation, GRC, and future applications.

**Scope**
- Tenant/application isolation penetration tests and performance tuning.
- SLOs, runbooks, on-call alerts, and dashboard standardization.
- Developer portal artifacts: API specs, event catalog, onboarding guides.
- First two business systems onboarded with shared platform services.

**Acceptance criteria**
- Isolation tests demonstrate no cross-tenant leakage under load.
- Published operational SLOs with alert thresholds and response playbooks.
- Office Automation and GRC consume platform APIs/events without forks.
- Platform change process enforces backward compatibility for APIs/events.

---

## Delivery Governance (applies to all phases)
- **Definition of Done** includes: security review, observability checks, migration scripts, and contract tests.
- **Compatibility policy**: no breaking API/event changes without version increment.
- **Data policy**: all relevant tables/events include `TenantId` + `ApplicationId`.
- **Architecture fitness checks**: automated tests for query filters, outbox publishing, and idempotent consumers.
