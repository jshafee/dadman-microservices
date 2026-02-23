namespace BuildingBlocks.Messaging;

public static class PlatformEventEnvelopeFactory
{
    public static PlatformEventEnvelope<TPayload> Create<TPayload>(
        string eventType,
        Guid tenantId,
        Guid applicationId,
        string correlationId,
        PlatformEventActor actor,
        PlatformEventProducer producer,
        TPayload payload,
        string? causationId = null,
        int version = 1)
        => new(
            Guid.NewGuid(),
            eventType,
            version,
            DateTimeOffset.UtcNow,
            tenantId,
            applicationId,
            correlationId,
            causationId,
            actor,
            producer,
            payload);
}
