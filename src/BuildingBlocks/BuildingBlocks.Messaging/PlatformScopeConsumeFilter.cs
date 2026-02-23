using BuildingBlocks.Scoping;
using MassTransit;

namespace BuildingBlocks.Messaging;

public sealed class PlatformScopeConsumeFilter<TPayload>(ScopeContext scopeContext) : IFilter<ConsumeContext<PlatformEventEnvelope<TPayload>>> where TPayload : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("platformScope");
    }

    public Task Send(ConsumeContext<PlatformEventEnvelope<TPayload>> context, IPipe<ConsumeContext<PlatformEventEnvelope<TPayload>>> next)
    {
        var message = context.Message;
        if (message.TenantId == Guid.Empty || message.ApplicationId == Guid.Empty || string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            throw new InvalidOperationException("Platform event envelope is missing required scope metadata.");
        }

        scopeContext.TenantId = message.TenantId;
        scopeContext.ApplicationId = message.ApplicationId;
        scopeContext.CorrelationId = message.CorrelationId;
        scopeContext.CausationId = message.CausationId;
        scopeContext.PrincipalId = Guid.TryParse(message.Actor.Id, out var principalId) ? principalId : null;

        return next.Send(context);
    }
}
