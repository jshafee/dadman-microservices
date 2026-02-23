using BuildingBlocks.Messaging;

namespace Iam.Contracts;

public sealed record PermissionAssignedPayload(Guid PrincipalId, Guid PermissionId, Guid TenantId, Guid ApplicationId);

public sealed record MembershipUpdatedPayload(Guid PrincipalId, string MembershipType, Guid MembershipId, Guid TenantId, Guid ApplicationId);

public sealed record PermissionAssignedEvent(PlatformEventEnvelope<PermissionAssignedPayload> Envelope);

public sealed record MembershipUpdatedEvent(PlatformEventEnvelope<MembershipUpdatedPayload> Envelope);
