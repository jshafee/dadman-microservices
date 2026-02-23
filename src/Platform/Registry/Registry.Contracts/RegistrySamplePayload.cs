using BuildingBlocks.Messaging;

namespace Registry.Contracts;

public sealed record TenantRegisteredPayload(Guid TenantId, string TenantCode, string TenantName, string Status);

public sealed record ApplicationRegisteredPayload(Guid ApplicationId, string ApplicationCode, string ApplicationName, string Status);

public sealed record TenantRegisteredEvent(PlatformEventEnvelope<TenantRegisteredPayload> Envelope);

public sealed record ApplicationRegisteredEvent(PlatformEventEnvelope<ApplicationRegisteredPayload> Envelope);
