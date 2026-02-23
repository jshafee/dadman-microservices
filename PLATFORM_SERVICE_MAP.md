# Platform Service Map (Platform-First)

## Principles
- **Platform-first reusable core**: capabilities are generic across Office Automation, GRC, and future systems.
- **Strict bounded contexts** with **database-per-service** and independent deployment.
- **Tenant + Application scope on all relevant resources** (`TenantId`, `ApplicationId`).
- **Event-first integration** for cross-service workflows; HTTP for command/query APIs.
- **No business-domain logic** (e.g., purchase requests, policy exceptions) inside platform services.

## Service Landscape

### 1) IAM Service
**Responsibility**
- Manage identities and authorization primitives: users, groups, roles, permissions, service principals.
- Persist effective grants and support application-scoped RBAC.
- Bridge external IdP identities to internal principals (authentication handled externally).
- Consume tenant/application lifecycle from Platform Registry (does not own tenant/app registration).

**Owned data (IAM DB)**
- `Users`, `UserExternalIdentities`, `Groups`, `GroupMembers`, `Roles`, `Permissions`, `RolePermissions`
- `UserRoleAssignments`, `GroupRoleAssignments`, `ServicePrincipals`, `ServicePrincipalSecrets/Keys`
- `PrincipalApplicationAccess`, `AuthorizationSnapshotVersions` (version pointer for fetch-on-demand)

### 1.1) Platform Registry Service
**Responsibility**
- Register and lifecycle-manage Tenants and Applications used by all platform services.

**Owned data (Registry DB)**
- `Tenants`, `Applications`, `TenantApplications`, `ApplicationLifecycleHistory`

**HTTP API (examples)**
- `POST /api/platform-registry/v1/tenants`
- `POST /api/platform-registry/v1/applications`
- `POST /api/platform-registry/v1/tenants/{tenantId}/applications/{applicationId}:enable`

**Domain events (examples)**
- `platform.registry.tenant.registered.v1`, `platform.registry.application.registered.v1`, `platform.registry.tenant-application.enabled.v1`

**HTTP API (examples)**
- `POST /api/iam/v1/users`
- `GET /api/iam/v1/users/{userId}`
- `POST /api/iam/v1/groups`, `POST /api/iam/v1/groups/{groupId}/members`
- `POST /api/iam/v1/roles`, `POST /api/iam/v1/roles/{roleId}/permissions`
- `POST /api/iam/v1/assignments:user-role`, `POST /api/iam/v1/assignments:group-role`
- `POST /api/iam/v1/service-principals`, `POST /api/iam/v1/service-principals/{id}/rotate-secret`
- `POST /api/iam/v1/authorization:check` (batch check for permission decisions)

**Domain events (examples)**
- `platform.iam.user.created.v1`, `platform.iam.group.member.added.v1`, `platform.iam.role.permission.granted.v1`
- `platform.iam.user.role.assigned.v1`, `platform.iam.service-principal.created.v1`

---

### 2) Organization Directory Service
**Responsibility**
- Model organization structures: org units, positions, assignments, reporting relationships.
- Maintain directory-backed membership semantics usable by workflow assignment and ACL resolution.

**Owned data (Org Directory DB)**
- `OrgUnits` (hierarchy), `Positions`, `PositionAssignments`, `ReportingLines`
- `DirectoryGroups` (if separated from IAM groups), `DirectoryMemberships`
- `CostCenters` / metadata extensions (optional generic attributes)

**HTTP API (examples)**
- `POST /api/org/v1/org-units`, `PATCH /api/org/v1/org-units/{id}`
- `POST /api/org/v1/positions`, `POST /api/org/v1/positions/{id}/assignments`
- `GET /api/org/v1/users/{userId}/directory-context`
- `GET /api/org/v1/org-units/{id}/members?includeDescendants=true`

**Domain events (examples)**
- `platform.org.org-unit.created.v1`, `platform.org.position.created.v1`, `platform.org.assignment.changed.v1`
- `platform.org.membership.updated.v1`

---

### 3) Document Management Service
**Responsibility**
- Manage document metadata, lifecycle states, version pointers, ACLs, tags, retention markers.
- Store binary blobs in object storage (S3/MinIO/Azure Blob), not relational DB.
- Provide presigned upload/download workflow and ACL-aware retrieval.

**Owned data (Document DB)**
- `Documents`, `DocumentVersions`, `DocumentMetadata`, `DocumentTags`
- `DocumentAclEntries` (principal/group/role based), `DocumentRetentionPolicies`
- `StorageObjects` (bucket/key/checksum/content-type/reference)
- `OwnerResourceType`, `OwnerResourceId` on `Documents` for polymorphic ownership

**HTTP API (examples)**
- `POST /api/docs/v1/documents` (create metadata)
- `POST /api/docs/v1/documents/{id}/versions:init-upload` (returns presigned URL)
- `POST /api/docs/v1/documents/{id}/versions/{versionId}:complete-upload`
- `GET /api/docs/v1/documents/{id}`
- `POST /api/docs/v1/documents/{id}/acl`
- `POST /api/docs/v1/documents:search`

**Domain events (examples)**
- `platform.docs.document.created.v1`, `platform.docs.document.version.uploaded.v1`
- `platform.docs.document.acl.updated.v1`, `platform.docs.document.deleted.v1`

---

### 4) Workflow Service
**Responsibility**
- Generic workflow templates/definitions, instances, tasks, approvals, reassignments, escalations.
- User inbox/task querying and assignment rules (RBAC/group/org-unit/position aware), with runtime user resolution.
- Integrate with external business services via events and callback endpoints.

**Owned data (Workflow DB)**
- `WorkflowDefinitions`, `WorkflowDefinitionVersions`
- `WorkflowInstances`, `WorkflowSteps`, `WorkflowTasks`, `TaskAssignments`
- `ApprovalDecisions`, `EscalationRules`, `InboxProjections`

**HTTP API (examples)**
- `POST /api/workflow/v1/definitions`
- `POST /api/workflow/v1/instances`
- `GET /api/workflow/v1/inbox/tasks?assignee={principalId}`
- `POST /api/workflow/v1/tasks/{taskId}:claim`
- `POST /api/workflow/v1/tasks/{taskId}:complete`
- `POST /api/workflow/v1/tasks/{taskId}:reassign`

**Domain events (examples)**
- `platform.workflow.instance.started.v1`, `platform.workflow.task.created.v1`
- `platform.workflow.task.completed.v1`, `platform.workflow.instance.completed.v1`, `platform.workflow.instance.cancelled.v1`

---

### 5) Audit Trail Service
**Responsibility**
- Append-only immutable trail for business and security-relevant actions.
- Ingest platform/domain events and explicit access events (e.g., document view/download).
- Provide query APIs for compliance, investigations, and evidence extraction.

**Owned data (Audit DB)**
- `AuditRecords` (append-only), `AuditRecordHashes` (integrity chain)
- `AuditIngestionCheckpoints`, `EvidenceExports`

**HTTP API (examples)**
- `POST /api/audit/v1/records:ingest` (for explicit events)
- `POST /api/audit/v1/access-events`
- `GET /api/audit/v1/records?subjectId=&action=&from=&to=`
- `POST /api/audit/v1/evidence-exports`

**Domain events (examples)**
- `platform.audit.record.appended.v1`, `platform.audit.evidence-export.generated.v1`

---

### 6) Notification Service (initially worker-centric)
**Responsibility**
- Template management, channel abstraction (email, SMS, in-app/webhook later), delivery orchestration.
- Delivery status tracking, retries, dead-letter handling, idempotency.

**Owned data (Notification DB)**
- `NotificationTemplates`, `TemplateVersions`
- `NotificationMessages`, `DeliveryAttempts`, `DeliveryReceipts`
- `ChannelConfigs` (tenant/app scoped)

**HTTP API (examples)**
- `POST /api/notify/v1/templates`
- `POST /api/notify/v1/messages`
- `GET /api/notify/v1/messages/{id}/status`
- `POST /api/notify/v1/messages:preview`

**Domain events (examples)**
- `platform.notify.message.queued.v1`, `platform.notify.delivery.succeeded.v1`, `platform.notify.delivery.failed.v1`

---

## Cross-Cutting Platform Components
- **Gateway (YARP) + Token Exchange**: validate external token and issue internal JWT carrying canonical `principal_id`, `tenant_id`, `application_id`.
- **Service bus (MassTransit + RabbitMQ)**: async integration, retries, outbox/inbox.
- **Observability (OpenTelemetry + Seq)**: trace correlation across HTTP + events.
- **Data consistency**: EF Core outbox/inbox per service for at-least-once event handling with idempotency.

## Bounded Context Interaction Rules
- Services access only their own database.
- Inter-service data sharing via:
  1. HTTP APIs for synchronous needs (authorization checks, query-on-demand).
  2. Domain events for async state propagation/projections.
- No service may store another service's mutable aggregate as source of truth.
