namespace BuildingBlocks.Messaging;

public sealed record PlatformEventEnvelope<TPayload>(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAtUtc,
    Guid TenantId,
    Guid ApplicationId,
    string CorrelationId,
    string? CausationId,
    PlatformEventActor Actor,
    PlatformEventProducer Producer,
    TPayload Payload);

public sealed record PlatformEventActor(string Type, string Id);
public sealed record PlatformEventProducer(string Service, string InstanceId);
