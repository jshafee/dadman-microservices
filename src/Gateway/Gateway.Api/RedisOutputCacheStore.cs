using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;

namespace Gateway.Api;

public sealed class RedisOutputCacheStore(IConnectionMultiplexer connectionMultiplexer) : IOutputCacheStore
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var tagKey = ToTagKey(tag);
            var tagMembers = await _database.SetMembersAsync(tagKey).ConfigureAwait(false);
            if (tagMembers.Length == 0)
            {
                return;
            }

            var cacheKeys = tagMembers.Select(member => (RedisKey)member.ToString()).ToArray();
            await _database.KeyDeleteAsync(cacheKeys).ConfigureAwait(false);
            await _database.KeyDeleteAsync(tagKey).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Cache store is best-effort. Ignore Redis outages to keep request pipeline available.
        }
    }

    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var value = await _database.StringGetAsync(ToCacheKey(key)).ConfigureAwait(false);
            return value.HasValue ? (byte[]?)value : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Treat Redis failures like cache misses.
            return null;
        }
    }

    public async ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var cacheKey = ToCacheKey(key);
            var transaction = _database.CreateTransaction();

            _ = transaction.StringSetAsync(cacheKey, value, validFor);

            if (tags is { Length: > 0 })
            {
                foreach (var tag in tags)
                {
                    var tagKey = ToTagKey(tag);
                    _ = transaction.SetAddAsync(tagKey, cacheKey.ToString());
                    _ = transaction.KeyExpireAsync(tagKey, validFor);
                }
            }

            await transaction.ExecuteAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Cache store is best-effort. Ignore Redis outages to keep request pipeline available.
        }
    }

    private static RedisKey ToCacheKey(string key) => $"output-cache:entry:{key}";

    private static RedisKey ToTagKey(string tag) => $"output-cache:tag:{tag}";
}
