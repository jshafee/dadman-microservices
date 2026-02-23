# Platform Data Scoping Model

## 1) Scope Dimensions
Every relevant platform resource MUST carry:
- `TenantId` (organization/customer boundary)
- `ApplicationId` (solution boundary inside tenant, e.g., OfficeAutomation, GRC)

Recommended additional columns:
- `ResourceId` (entity key)
- `CreatedAtUtc`, `CreatedBy`
- `LastModifiedAtUtc`, `LastModifiedBy`
- `RowVersion` (concurrency)

### Scope rules
1. **Tenant isolation is hard boundary** (no cross-tenant reads/writes).
2. **Application isolation is default boundary** within a tenant.
3. Allow controlled cross-application access only via explicit platform policy and audited operations.

## 2) Canonical Scope Envelope
All APIs and events should include a canonical scope envelope:

```json
{
  "scope": {
    "tenantId": "tnt_...",
    "applicationId": "app_...",
    "correlationId": "...",
    "causationId": "..."
  }
}
```

- `correlationId`: trace end-to-end request/workflow.
- `causationId`: link immediate parent action/event.

## 3) Claims Model (from external IdP + platform augmentation)
Authentication comes from external IdP; gateway performs token exchange and issues an internal platform JWT consumed by services.

### Required internal platform token claims
- `sub`: external subject
- `principal_id`: internal principal identifier
- `tenant_id`: tenant identifier
- `application_id`: current client application
- `azp` or `client_id`: calling app/service principal
- `permission_snapshot_version`: compact auth snapshot pointer
- `scope` / `scp` (optional for coarse OAuth scope)

### Platform-resolved claims/context
Resolved by token exchange + authz middleware (cached):
- `principal_id`
- `tenant_id`
- `application_id`
- `permission_snapshot_version`
- `group_ids[]`
- `service_principal_id` (if machine identity)

## 4) HTTP API Scoping Conventions
- Require `TenantId` and `ApplicationId` from token claims by default.
- Do not accept arbitrary tenant/application from request body unless caller has elevated platform admin permission.
- Canonical identity/scope source is the internal JWT; gateway headers are diagnostic only.
- Include scope in response payload headers (optional):
  - `X-Tenant-Id`
  - `X-Application-Id`
  - `X-Correlation-Id`

### Example endpoint behavior
- `POST /api/docs/v1/documents`
  - Server stamps `TenantId`, `ApplicationId` from authenticated context.
- `GET /api/workflow/v1/inbox/tasks`
  - Returns tasks only in caller's tenant+application unless explicit cross-app permission exists.

## 5) EF Core Query Filter Approach
Use global query filters per scoped aggregate.

### Base entity interfaces
- `ITenantScoped { Guid TenantId; }`
- `IApplicationScoped { Guid ApplicationId; }`
- `ISoftDelete` (optional)

### DbContext pattern
- Inject `IScopeContext` (tenant/application from current request/message).
- Apply global filters:
  - `e => e.TenantId == scope.TenantId && e.ApplicationId == scope.ApplicationId`
- For system-level jobs, use explicit privileged context with audit logging.

### Guardrails
- Disallow `IgnoreQueryFilters()` in application layer except vetted repository methods.
- Add automated tests asserting scope isolation for each service repository.

## 6) Messaging/Event Scoping Conventions
All published events include:
- `eventId`, `eventType`, `occurredAtUtc`, `version`
- `tenantId`, `applicationId`
- `correlationId`, `causationId`
- `actor` (`userId` or `servicePrincipalId`)
- `producer.service`, `producer.instanceId`

Canonical mapping choice:
- Scope and identity fields are part of event wrapper payload (not headers-only contracts).
- MassTransit consumers set `IScopeContext` via a shared consume filter reading the wrapper envelope.

Consumers MUST:
- Validate scope presence.
- Enforce scoped writes in inbox handler.
- Reject/park messages with missing or invalid scope metadata.
- Treat transport headers as diagnostic hints, not source of truth.

## 7) Authorization Evaluation Order
For any resource access:
1. Validate token and principal.
2. Resolve platform principal and effective permissions.
3. Enforce tenant/application scope.
4. Evaluate resource ACL/business rule (if applicable).
5. Emit explicit audit event for sensitive reads/exports.

## 8) Cross-Application Access Pattern (Controlled)
Use a dedicated permission such as `platform.cross_application.read` / `platform.cross_application.write`.

Constraints:
- Explicitly declared allow-list of target `ApplicationId`s.
- Mandatory audit trail with justification metadata.
- Optional approval workflow for privileged operations.

## 9) Data Migration and Backfill Guidance
When onboarding existing systems:
- Backfill `TenantId` and `ApplicationId` for all platform entities.
- Quarantine records lacking deterministic scope.
- Prevent go-live until 100% scoped coverage validation passes.

## 10) Observability and Diagnostics
Attach scope attributes to logs/traces/metrics:
- `tenant.id`, `application.id`, `principal.id`, `correlation.id`

This enables per-tenant/app SLOs, anomaly detection, and incident investigations.
