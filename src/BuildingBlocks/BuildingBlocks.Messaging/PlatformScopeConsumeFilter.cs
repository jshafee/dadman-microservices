using BuildingBlocks.Scoping;
using MassTransit;

namespace BuildingBlocks.Messaging;

public sealed class PlatformEnvelopeScopeConsumeFilter<TMessage>(ScopeContext scopeContext, bool requireTenantApplicationScope = true) : IFilter<ConsumeContext<TMessage>>
    where TMessage : class, IPlatformEventEnvelope
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("platformScope");
    }

    public Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        var message = context.Message;

        if (requireTenantApplicationScope && (!message.TenantId.HasValue || !message.ApplicationId.HasValue))
        {
            throw new InvalidOperationException("Platform event envelope is missing required tenant/application scope metadata.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            throw new InvalidOperationException("Platform event envelope is missing required correlation metadata.");
        }

        scopeContext.TenantId = message.TenantId;
        scopeContext.ApplicationId = message.ApplicationId;
        scopeContext.CorrelationId = message.CorrelationId;
        scopeContext.CausationId = message.CausationId;
        scopeContext.PrincipalId = Guid.TryParse(message.Actor.Id, out var principalId) ? principalId : null;

        return next.Send(context);
    }
}

public sealed class PlatformScopeConsumeFilter<TPayload>(ScopeContext scopeContext, bool requireTenantApplicationScope = true)
    : IFilter<ConsumeContext<PlatformEventEnvelope<TPayload>>>
{
    private readonly PlatformEnvelopeScopeConsumeFilter<PlatformEventEnvelope<TPayload>> _inner =
        new(scopeContext, requireTenantApplicationScope);

    public void Probe(ProbeContext context) => _inner.Probe(context);

    public Task Send(ConsumeContext<PlatformEventEnvelope<TPayload>> context, IPipe<ConsumeContext<PlatformEventEnvelope<TPayload>>> next)
        => _inner.Send(context, next);
}
