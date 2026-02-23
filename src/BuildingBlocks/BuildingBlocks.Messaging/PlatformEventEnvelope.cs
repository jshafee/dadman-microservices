using System.Text.Json.Serialization;

namespace BuildingBlocks.Messaging;

public interface IPlatformEventEnvelope
{
    Guid EventId { get; }
    string EventType { get; }
    int Version { get; }
    DateTimeOffset OccurredAtUtc { get; }
    Guid? TenantId { get; }
    Guid? ApplicationId { get; }
    string CorrelationId { get; }
    string? CausationId { get; }
    ActorInfo Actor { get; }
    ProducerInfo Producer { get; }
}

public sealed record PlatformEventEnvelope<TPayload>(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("occurredAtUtc")] DateTimeOffset OccurredAtUtc,
    [property: JsonPropertyName("tenantId")] Guid? TenantId,
    [property: JsonPropertyName("applicationId")] Guid? ApplicationId,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("causationId")] string? CausationId,
    [property: JsonPropertyName("actor")] ActorInfo Actor,
    [property: JsonPropertyName("producer")] ProducerInfo Producer,
    [property: JsonPropertyName("payload")] TPayload Payload) : IPlatformEventEnvelope;

public record ActorInfo(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id);

public record ProducerInfo(
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("instanceId")] string InstanceId);

public record PlatformEventActor(string Type, string Id) : ActorInfo(Type, Id);

public record PlatformEventProducer(string Service, string InstanceId) : ProducerInfo(Service, InstanceId);
