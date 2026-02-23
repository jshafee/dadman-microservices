# Platform Event Catalog v1

This catalog defines core platform events for IAM, Organization Directory, Documents, Workflow, Audit, and Notifications.
All events are integration events published via message bus with outbox reliability.

## Standard Envelope (all events)
```json
{
  "eventId": "uuid",
  "eventType": "platform.<context>.<event>.v1",
  "version": 1,
  "occurredAtUtc": "2026-01-01T12:00:00Z",
  "tenantId": "uuid",
  "applicationId": "uuid",
  "correlationId": "uuid",
  "causationId": "uuid",
  "actor": {
    "type": "user|servicePrincipal|system",
    "id": "uuid|string"
  },
  "producer": {
    "service": "string",
    "instanceId": "string"
  },
  "payload": {}
}
```

**Canonical transport mapping**
- Envelope is carried in message payload as wrapper contract.
- Broker/transport headers may duplicate correlation fields for diagnostics only.

## Platform Registry Events

### 0.1) `platform.registry.tenant.registered.v1`
**Payload fields**
- `tenantId` (guid)
- `tenantCode` (string)
- `tenantName` (string)
- `status` (Active|Disabled)

### 0.2) `platform.registry.application.registered.v1`
**Payload fields**
- `applicationId` (guid)
- `applicationCode` (string)
- `applicationName` (string)
- `status` (Active|Disabled)

### 0.3) `platform.registry.tenant-application.enabled.v1`
**Payload fields**
- `tenantId` (guid)
- `applicationId` (guid)
- `enabledBy` (guid|string)
- `enabledAtUtc` (datetime)

## IAM Events

### 1) `platform.iam.user.created.v1`
**Payload fields**
- `userId` (guid)
- `externalSubject` (string)
- `displayName` (string)
- `email` (string)
- `status` (Active|Disabled)

### 2) `platform.iam.group.created.v1`
**Payload fields**
- `groupId` (guid)
- `name` (string)
- `description` (string?)

### 3) `platform.iam.group.member.added.v1`
**Payload fields**
- `groupId` (guid)
- `principalType` (User|ServicePrincipal)
- `principalId` (guid)
- `membershipType` (Direct|Inherited)

### 4) `platform.iam.role.permission.granted.v1`
**Payload fields**
- `roleId` (guid)
- `permissionCode` (string)
- `grantType` (Allow|Deny)

### 5) `platform.iam.user.role.assigned.v1`
**Payload fields**
- `userId` (guid)
- `roleId` (guid)
- `assignmentScope` (Tenant|Application)
- `expiresAtUtc` (datetime?)

### 6) `platform.iam.service-principal.created.v1`
**Payload fields**
- `servicePrincipalId` (guid)
- `clientId` (string)
- `displayName` (string)
- `status` (Active|Disabled)

## Organization Directory Events

### 7) `platform.org.org-unit.created.v1`
**Payload fields**
- `orgUnitId` (guid)
- `parentOrgUnitId` (guid?)
- `code` (string)
- `name` (string)

### 8) `platform.org.position.created.v1`
**Payload fields**
- `positionId` (guid)
- `orgUnitId` (guid)
- `title` (string)
- `positionCode` (string)

### 9) `platform.org.assignment.changed.v1`
**Payload fields**
- `assignmentId` (guid)
- `positionId` (guid)
- `userId` (guid)
- `action` (Assigned|Unassigned|Replaced)
- `effectiveFromUtc` (datetime)
- `effectiveToUtc` (datetime?)

### 9.1) `platform.org.membership.updated.v1`
**Payload fields**
- `membershipId` (guid)
- `groupId` (guid)
- `principalType` (User|ServicePrincipal|Position|OrgUnit)
- `principalId` (guid|string)
- `action` (Added|Removed|Updated)

## Document Management Events

### 10) `platform.docs.document.created.v1`
**Payload fields**
- `documentId` (guid)
- `documentType` (string)
- `title` (string)
- `ownerResourceType` (User|Group|OrgUnit|Position|WorkflowInstance|Service)
- `ownerResourceId` (guid|string)
- `ownerPrincipalId` (guid, optional convenience)
- `classification` (Public|Internal|Confidential|Restricted)

### 11) `platform.docs.document.version.uploaded.v1`
**Payload fields**
- `documentId` (guid)
- `versionId` (guid)
- `versionNumber` (int)
- `storageProvider` (string)
- `objectKey` (string)
- `contentType` (string)
- `sizeBytes` (long)
- `checksumSha256` (string)

### 12) `platform.docs.document.acl.updated.v1`
**Payload fields**
- `documentId` (guid)
- `aclVersion` (int)
- `changes` (array of {principalType, principalId, permission, action})

### 13) `platform.docs.document.accessed.v1`
**Payload fields**
- `documentId` (guid)
- `accessType` (View|Download|Preview|Share)
- `accessResult` (Allowed|Denied)
- `clientIp` (string?)
- `userAgent` (string?)

### 13.1) `platform.docs.document.deleted.v1`
**Payload fields**
- `documentId` (guid)
- `deletedBy` (guid|string)
- `deletedAtUtc` (datetime)
- `reason` (string?)

## Workflow Events

### 14) `platform.workflow.instance.started.v1`
**Payload fields**
- `workflowInstanceId` (guid)
- `definitionId` (guid)
- `definitionVersion` (int)
- `businessKey` (string)
- `initiatorPrincipalId` (guid)

### 15) `platform.workflow.task.created.v1`
**Payload fields**
- `taskId` (guid)
- `workflowInstanceId` (guid)
- `taskType` (Approval|Review|Action)
- `name` (string)
- `candidatePrincipals` (guid[], optional explicit principals)
- `candidateRoles` (string[])
- `candidateGroups` (guid[])
- `candidateOrgUnits` (guid[])
- `candidatePositions` (guid[])
- `resolvedCandidatePrincipals` (guid[], optional inbox projection cache)
- `dueAtUtc` (datetime?)

### 16) `platform.workflow.task.completed.v1`
**Payload fields**
- `taskId` (guid)
- `workflowInstanceId` (guid)
- `decision` (Approved|Rejected|Completed)
- `outcomeCode` (string?)
- `comments` (string?)
- `completedBy` (guid)

### 17) `platform.workflow.instance.completed.v1`
**Payload fields**
- `workflowInstanceId` (guid)
- `completionStatus` (Completed|Rejected|Cancelled)
- `completedAtUtc` (datetime)

### 17.1) `platform.workflow.instance.cancelled.v1`
**Payload fields**
- `workflowInstanceId` (guid)
- `cancelledBy` (guid|string)
- `cancelledAtUtc` (datetime)
- `reason` (string?)

## Audit Events

### 18) `platform.audit.record.appended.v1`
**Payload fields**
- `auditRecordId` (guid)
- `category` (Security|DataAccess|Admin|Workflow)
- `action` (string)
- `subjectType` (User|Document|Workflow|Role|Group)
- `subjectId` (string)
- `integrityHash` (string)

### 19) `platform.audit.evidence-export.generated.v1`
**Payload fields**
- `exportId` (guid)
- `requestedBy` (guid)
- `filters` (object)
- `recordCount` (int)
- `artifactUri` (string)

## Notification Events

### 20) `platform.notify.message.queued.v1`
**Payload fields**
- `messageId` (guid)
- `templateId` (guid)
- `channel` (Email|Sms|InApp|Webhook)
- `recipient` (string)
- `priority` (Low|Normal|High)

### 21) `platform.notify.delivery.succeeded.v1`
**Payload fields**
- `messageId` (guid)
- `channel` (string)
- `providerMessageId` (string)
- `deliveredAtUtc` (datetime)

### 22) `platform.notify.delivery.failed.v1`
**Payload fields**
- `messageId` (guid)
- `channel` (string)
- `failureCode` (string)
- `failureReason` (string)
- `attemptNumber` (int)
- `nextRetryAtUtc` (datetime?)

---

## Versioning and Compatibility Rules
- Additive payload changes are backward-compatible.
- Breaking changes require new event name/version suffix (e.g., `.v2`).
- Consumers must ignore unknown fields.
- Producers must preserve required fields and scope envelope.
