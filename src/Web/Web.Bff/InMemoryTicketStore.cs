using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.Bff;

public sealed class InMemoryTicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, AuthenticationTicket> _store = new();

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"ticket-{Guid.NewGuid():N}";
        _store[key] = ticket;
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        _store[key] = ticket;
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _store.TryGetValue(key, out var ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
